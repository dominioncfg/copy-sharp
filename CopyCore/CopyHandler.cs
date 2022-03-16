using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Forms;

namespace CopyCore
{
    [Serializable]
    public class DirectoryToBigException : Exception
    {
        public long Needed
        {
            get;
            set;
        }
        public ICopierProgress Trigger
        {
            get;
            set;
        }

        public DirectoryToBigException(ICopierProgress Trigger, long Needed)
            : base("No hay suficiente espacio para copiar el directorio.")
        {
            this.Trigger = Trigger;
            this.Needed = Needed;
        }
        public DirectoryToBigException(string message) : base(message) { }
        public DirectoryToBigException(string message, Exception inner) : base(message, inner) { }
        protected DirectoryToBigException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    #region EventArgs
    public class CopyEndedEventArgs : EventArgs
    {

    }
    #endregion

    #region Enumeraciones
    public enum SortDirection
    {
        Ascending,
        Descending
    }

    public enum SortField
    {
        FileName, Size, FromFolder, DestinationFolder
    }
    #endregion

    #region Comparers
    public class LengthComparer : IComparer<FileCopier>
    {

        public int Compare(FileCopier x, FileCopier y)
        {
            return x.TotalLength.CompareTo(y.TotalLength);
        }
    }

    public class FromFolderComparer : IComparer<FileCopier>
    {

        public int Compare(FileCopier x, FileCopier y)
        {
            return x.FromFolder.CompareTo(y.FromFolder);
        }
    }

    public class DestinationFolderComparer : IComparer<FileCopier>
    {

        public int Compare(FileCopier x, FileCopier y)
        {
            return x.DestinationFolder.CompareTo(y.DestinationFolder);
        }
    }

    public class FileNameComparer : IComparer<FileCopier>
    {

        public int Compare(FileCopier x, FileCopier y)
        {
            return x.FileFriendlyName.CompareTo(y.FileFriendlyName);
        }
    }
    #endregion

    /// <summary>
    /// Maneja la copia de muchos ficheros.
    /// </summary>
    /// 
    public class CopyHandler : ObservableCollection<FileCopier>, ICopierProgress
    {

        #region Campos
        /// <summary>
        /// Carpeta a la cual se va a copiar.
        /// </summary>
        private DirectoryInfo destination;

        /// <summary>
        /// El ficehro actual que se esta copiando.
        /// </summary>
        private FileCopier actual;

        /// <summary>
        /// El Progreso Total de la copia.
        /// </summary>
        private int progress;

        /// <summary>
        /// La velocidad de la copia.
        /// </summary>
        private long speed;

        /// <summary>
        /// Lo total que se va copiar.
        /// </summary>
        private long totalLength;

        /// <summary>
        /// Cantitad total de ficheros que hay en esta copia.
        /// </summary>
        private int filesCount;

        /// <summary>
        /// La cantidad de ficheros que ya han sido copiados.
        /// </summary>
        private int endedFilesCount;

        //*Cacular la cantidad de bytes copiados*///
        /// <summary>
        /// Copiado hasta el fichero actual
        /// </summary>
        private long beforeFiles;

        /// <summary>
        /// Cuanto se ha copiado del fichero actual.
        /// </summary>
        private long actFileCopiedBytes;
        //**End Cacular la cantidad de bytes copiados*//

  
        /// <summary>
        /// Guardar el estado de la copia
        /// </summary>
        private CopyState state;

        #endregion

        #region Propiedades
        /// <summary>
        /// El fichero actual.
        /// </summary>
        public FileCopier Actual
        {
            get { return actual; }
            protected set
            {                
                actual = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Actual"));
            }
        }

        /// <summary>
        /// La carpeta destino por defecto
        /// </summary>
        public DirectoryInfo Destination
        {
            get { return destination; }
            protected set
            {               
                destination = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Destination"));
            }
        }

        /// <summary>
        /// El progreso de la copia.
        /// </summary>
        public int Progress
        {
            get { return progress; }
            protected set
            {
                progress = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Progress"));                
            }
        }

        /// <summary>
        /// La velocidad de la copia;
        /// </summary>
        public long Speed
        {
            get { return speed; }
            protected set
            {
                speed = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Speed"));
                OnPropertyChanged(new PropertyChangedEventArgs("RemainingTime"));
                OnPropertyChanged(new PropertyChangedEventArgs("BytesCopied"));
                
            }
        }

        /// <summary>
        /// El tiempo que queda
        /// </summary>
        public TimeSpan RemainingTime
        {
            get { return TimeRemainingCalculator.Calculate(this); }
        }

        /// <summary>
        /// El espacio total a copiar
        /// </summary>
        public long TotalLength
        {
            get { return totalLength; }
            protected set
            {                
                totalLength = value;
                OnPropertyChanged(new PropertyChangedEventArgs("TotalLength"));
            }
        }

        /// <summary>
        /// La cantidad de archivos de la copia en general
        /// </summary>
        public int FilesCount
        {
            get { return filesCount; }
            protected set
            {               
                filesCount = value;
                OnPropertyChanged(new PropertyChangedEventArgs("FilesCount"));
            }
        }

        /// <summary>
        /// El espacio que se ha copiado ya
        /// </summary>
        public long BytesCopied
        {
            get { return beforeFiles + actFileCopiedBytes; }
        }

        /// <summary>
        /// Cantidad de ficheros copiados.
        /// </summary>
        public int CopiedFiles
        {
            get { return this.endedFilesCount; }
            protected set
            {
                endedFilesCount = value;
                OnPropertyChanged(new PropertyChangedEventArgs("CopiedFiles"));
            }
        }

        /// <summary>
        /// Guardar el estado de la copia
        /// </summary>
        public CopyState State
        {
            get
            {
                return state;
            }
            protected set
            {
                if (value != state)
                {
                
                    this.state = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("State"));
                    OnPropertyChanged(new PropertyChangedEventArgs("IsLoading"));
                }
            }
        }

        public bool IsLoading
        {
            get
            {
                return this.State == CopyState.NoStartedYet;
            }
        }
        #endregion

        #region Delegados
        /// <summary>
        /// Para cuando se acabe la copia.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void CopyCompletedEventHandler(object sender, CopyEndedEventArgs e);
        #endregion

        #region Eventos
        /// <summary>
        /// Se lanza cuando se acaba la copia
        /// </summary>
        public event CopyCompletedEventHandler CopyEnds;

        
        #endregion

        #region Constructores

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Destination">Para donde van los ficheros</param>
        public CopyHandler(DirectoryInfo Destination)
            : base()
        {
            this.destination = Destination;
        }

        /// <summary>
        /// Crear Un copiador. De una carpeta a otra
        /// </summary>
        /// <param name="Source">De donde</param>
        /// <param name="Destination">Para donde van los ficheros</param>
        public CopyHandler(DirectoryInfo Source, DirectoryInfo Destination)
            : this(Destination)
        {

            AddFolder(Source);
        }
        #endregion

        #region Metodos Privados
        /// <summary>
        /// El espacio a tener en cuaenta antes de agregar.
        /// </summary>
        /// <param name="RequiredLength"></param>
        /// <returns></returns>
        private long RequiredSpace(long RequiredLength)
        {
            long ActualCopyng = 0;
            if (Actual != null)
            {
                ActualCopyng = this.Actual.TotalLength;
            }
            return (this.TotalLength - (this.beforeFiles + ActualCopyng)) + RequiredLength;
        }

        /// <summary>
        /// Deveuelve todos los archivos que hay en una carpeta y devuelve su tamaño.
        /// </summary>
        /// <param name="Path">La ubicacion de la carpeta</param>
        /// <param name="FilesInFolders">Donde Van e</param>
        /// <returns>El tamaño completo del directiorio</returns>
        private long GetAllFilesInFolder(DirectoryInfo Path, ref List<FileInfo> FilesInFolders)
        {
            long l = 0;

            foreach (FileInfo item in Path.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                l += item.Length;
                FilesInFolders.Add(item);
            }
            return l;
        }

        /// <summary>
        /// Salta al siguiente fichero.
        /// </summary>
        private void NextFile()
        {
            if (this.Count == 0)
            {
                /*Aqui hay que hacer algo mas*/
                this.Progress = 100;
                OnCopyCompleted();
                return;
            }
            Actual = this[0];
            Actual.Completed += fc_Completed;
            Actual.ProgressChanged += Actual_ProgressChanged;            
            this.RemoveAt(0);

            this.beforeFiles += actFileCopiedBytes;
            if (this.State==CopyState.Copying)
            {
                Actual.StarCopy();    
            }
            
        }

     
        #endregion

        #region Metodos Publicos
        /// <summary>
        /// Sube un nivel a un archivo
        /// </summary>
        /// <param name="index"></param>
        public void UpOneLevel(FileCopier Target)
        {
            int index = this.IndexOf(Target);

            if (index<=0||index>=Count)
            {
                return;
            }
            
            FileCopier tmp = this[index - 1];
            this[index - 1] = this[index];
            this[index] = tmp;
        }
        /// <summary>
        /// Pone a un archivo de primero
        /// </summary>
        /// <param name="index"></param>
        public void UpAllLevels(FileCopier Target)
        {
            int index = this.IndexOf(Target);
            if (index <= 0 || index >= Count)
            {
                return;
            }            

            FileCopier tmp = this[index];
            this.RemoveAt(index);
            this.Insert(0,tmp);
        }

        /// <summary>
        /// Baja un nivel a un archivo
        /// </summary>
        /// <param name="index"></param>
        public void DownOneLevel(FileCopier Target)
        {
            int index = this.IndexOf(Target);
            if (index < 0 || index >= Count)
            {
                return;
            }

            FileCopier tmp = this[index + 1];
            this[index + 1] = this[index];
            this[index] = tmp;
        }
        /// <summary>
        /// Pone a un archivo de primero
        /// </summary>
        /// <param name="index"></param>
        public void DownAllLevels(FileCopier Target)
        {
            int index = this.IndexOf(Target);
            if (index < 0 || index >= Count)
            {
                return;
            }

            FileCopier tmp = this[index];
            this.RemoveAt(index);
            this.Add(tmp);
        }

        /// <summary>
        /// Permite ordenar la cola de archivos
        /// </summary>
        /// <param name="Property"></param>
        /// <param name="Order"></param>
        public void SortBy(SortField Property,SortDirection Order) 
        {
            IComparer<FileCopier> CompToUse=null;
            int mul = 1;
            if (Order == SortDirection.Descending)
            {
                mul = -1;
            }
            switch (Property)
            {
                case SortField.FileName:
                    {
                        CompToUse = new FileNameComparer();
                    }
                    break;

                case SortField.Size:
                    {
                        CompToUse = new LengthComparer();
                    }
                    break;

                case SortField.FromFolder:
                    {
                        CompToUse = new FromFolderComparer();
                    }
                    break;

                case SortField.DestinationFolder:
                    {
                        CompToUse = new DestinationFolderComparer();
                    }
                    break;
            }

            for (int i = 0; i < this.Count; i++)
            {
                for (int j = i+1; j < this.Count; j++)
                {
                    if (mul*(CompToUse.Compare(this[i],this[j]))>0)
                    {
                        FileCopier tmp = this[i];
                        this[i] = this[j];
                        this[j] = tmp;
                    }
                }
            }
        }

        /// <summary>
        /// Agregar un fichero a la copia. (lo mete en la carpeta destino).
        /// </summary>
        /// <param name="ActFile">El fichero.</param>
        /// <param name="Source">De que carpeta viene.</param>
        public void AddFile(FileInfo ActFile, DirectoryInfo Source)
        {
            AddFile(ActFile, Source, Destination);
        }

        /// <summary>
        /// Agregar un fichero a la copia. En la carpteta dada.
        /// </summary>
        /// <param name="ActFile">El fichero.</param>
        /// <param name="Source">De que carpeta viene.</param>
        public void AddFile(FileInfo ActFile, DirectoryInfo Source, DirectoryInfo Destination)
        {
            /**Chekear*/


            DriveInfo HDD = new DriveInfo(Source.Root.ToString());
            long Required = RequiredSpace(ActFile.Length);
            if (HDD.TotalFreeSpace < Required)
            {
                FileToBigException ftbex = new FileToBigException(this, Required - HDD.TotalFreeSpace);
                throw ftbex;
            }
            /**End Chekear*/
            string Name = Source.FullName;
            Name = Name.TrimEnd(new char[] { '\\' });
            string SourceFullName = ActFile.Directory.FullName;
            string RemainingPath = SourceFullName.Replace(Name, "");
            string DestinationPath = Destination.FullName;
            string DestinationFileFullName = DestinationPath + RemainingPath;


            DirectoryInfo di = new DirectoryInfo(DestinationFileFullName);

            FileCopier fc = new FileCopier(ActFile, di);
            this.Add(fc);
            this.TotalLength += ActFile.Length;
            this.filesCount++;

        }
        /// <summary>
        /// Agregar Una Carpeta a la copia
        /// </summary>
        /// <param name="Source"></param>
        public void AddFolder(DirectoryInfo Source)
        {
            //los ficheros
            List<FileInfo> files = new List<FileInfo>();
            this.State = CopyState.NoStartedYet;
            /*Chekear*/
            long space = GetAllFilesInFolder(Source, ref files);
            DriveInfo HDD = new DriveInfo(Destination.Root.ToString());
            long Required = RequiredSpace(space);


            if (HDD.TotalFreeSpace < Required)
            {
                DirectoryToBigException ftbex = new DirectoryToBigException(this, Required - HDD.TotalFreeSpace);
                throw ftbex;
            }
            String Des = this.Destination.FullName + @"\" + Source.Name;
            DirectoryInfo d = new DirectoryInfo(Des);
            /*Chekear*/
            foreach (FileInfo item in files)
            {
                AddFile(item, Source, d);
            }
        }

        /// <summary>
        /// Empezar a copiar
        /// </summary>
        public void StarToCopy()
        {
            if (this.State == CopyState.Copying || this.State == CopyState.Canceled || this.State == CopyState.Completed)
            {
                return;
            }
            try
            {
                this.State = CopyState.Copying;
                NextFile();
            }
            catch (Exception ex)
            {
                this.State = CopyState.StandBy;
                throw ex;
            }
        }

        public void Play()
        {
            if (this.State == CopyState.Copying || this.State == CopyState.Canceled || this.State == CopyState.Completed)
            {
                return;
            }
            if (Actual == null)
            {
                return;
            }
            try
            {

                Actual.StarCopy();
                this.State = CopyState.Copying;
            }
            catch (Exception ex)
            {
                this.State = CopyState.StandBy;
                throw ex;
            }
        }

        /// <summary>
        /// Pausar La Copia
        /// </summary>
        public void Pause()
        {
            if (Actual != null && this.State == CopyState.Copying)
            {
                Actual.Pause();
                this.State = CopyState.Pasued;
            }
        }

        /// <summary>
        /// Cancelar la copia
        /// </summary>
        public void Cancel()
        {
            if (this.State == CopyState.Canceled || this.State == CopyState.Completed)
            {
                return;
            }
            if (Actual != null)
            {
                Actual.Cancel();
                this.State = CopyState.Canceled;

            }
        }

        /// <summary>
        /// Borrar Un Fichero
        /// </summary>
        public void Delete(FileCopier Target)
        {
            bool IsHere = this.Remove(Target);
            if (IsHere)
            {
                TotalLength -= Target.TotalLength;               
                FilesCount--;
            }                  

        }
        #endregion

        #region EventHandlers
        /// <summary>
        /// Cuando se termine de copiar cada fichero
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fc_Completed(object sender, FileCopyCompletedEventArgs e)
        {
            NextFile();
            CopiedFiles++;
        }

        /// <summary>
        /// Actualizar los datos de este objeto (VElocidad,Progreso,etc).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Actual_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.actFileCopiedBytes = Actual.BytesCopied;
            this.Speed = Actual.Speed;                            
            OnPropertyChanged(new PropertyChangedEventArgs("Actual"));
                   
            double p = ((double)BytesCopied) / ((double)TotalLength);
            p *= 100;
            this.Progress = (int)p;
        }
        #endregion

        #region Lanzar Eventos
        /// <summary>
        /// Cuando se complete la copia.
        /// </summary>
        public void OnCopyCompleted()
        {
            if (CopyEnds != null)
            {
                CopyEnds(this, new CopyEndedEventArgs());
            }
        }
        #endregion
    }

    
  
}
