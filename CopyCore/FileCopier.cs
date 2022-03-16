using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO;
//using System.Threading;
using System.Windows.Forms;
using System.Collections.Specialized;
namespace CopyCore
{
    #region Enumeraciones
    public enum CopyState
    {
        NoStartedYet, Copying, Pasued, StandBy, Completed, Canceled
    }
    #endregion

    #region Excepciones
    [Serializable]
    public class FileToBigException : Exception
    {
        public long Needed
        {
         get;set;   
        }
        public ICopierProgress Trigger
        {
            get;
            set;
        }

        public FileToBigException(ICopierProgress Trigger, long Needed)
            : base("No hay suficiente espacio para copiar el archivo") 
        {
            this.Trigger = Trigger;
            this.Needed = Needed;
        }
        public FileToBigException(string message) : base(message) { }
        public FileToBigException(string message, Exception inner) : base(message, inner) { }
        protected FileToBigException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class AlreadyExistException : Exception
    {
        public FileCopier Trigger
        {
            get;
            set;
        }

        public AlreadyExistException(FileCopier Trigger)
            : base("Ya existe el archivo.")
        {
            this.Trigger = Trigger;
        }
        public AlreadyExistException(string message) : base(message) { }
        public AlreadyExistException(string message, Exception inner) : base(message, inner) { }
        protected AlreadyExistException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
    #endregion

    #region EvetArgs
    /// <summary>
    /// EventArgs para el evento de Completed.
    /// </summary>
    public class FileCopyCompletedEventArgs : EventArgs
    {

    }
    /// <summary>
    /// Para informar a otros que se han actualizado los datos.
    /// </summary>
    public class DataUpdatedEventArgs : EventArgs
    {

    }

   

    #endregion


    /// <summary>
    /// Permite copiar un solo fichero hacia una carpeta en especifico.
    /// </summary>
    public class FileCopier : INotifyPropertyChanged, ICopierProgress
    {
        
        #region Delegados
        /// <summary>
        /// Delegado para el evento de Completed.
        /// </summary>
        /// <param name="sender">Referencia al FileCopier.</param>
        /// <param name="e">Informacion del evento.</param>
        public delegate void FileCopyCompletedEventHandler(object sender, FileCopyCompletedEventArgs e);
        /// <summary>
        /// delegado para el evento data updated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void DataUpdatedEventHandler(object sender, DataUpdatedEventArgs e);
        #endregion

        #region ConstantesEstaticas
        /// <summary>
        /// Capacidad del buffer de copiado.
        /// </summary>
        public const int BufferSize = 65536;
        /// <summary>
        /// El tiempo en que se actualizan algunos datos como la velocidad, el tiempo restante.
        /// </summary>
        public const int RefreshTime = 1000;
        #endregion

        #region Campos
        /// <summary>
        /// Fichero que se va a copiar.
        /// </summary>
        private FileInfo source;
        /// <summary>
        /// Carpeta a la cual se va a copiar.
        /// </summary>
        private DirectoryInfo destination;
        /// <summary>
        /// El que se encarga de hacer la copia.
        /// </summary>
        private BackgroundWorker worker;
        /// <summary>
        /// Porciento del progreso de la copia.
        /// </summary>
        private int progress;
        /// <summary>
        /// Que Parte del fichero se ha copiado.
        /// </summary>
        private long Offset;
        /// <summary>
        /// Para Calcular la velocidad de la copia
        /// </summary>
        private Timer speedTimer;
        /// <summary>
        /// La velocidad de la copia
        /// </summary>
        private long speed;
        /// <summary>
        /// Lo que se ha copiadod en el Tick Anterior( Para calcular el promedio de  velocidades)
        /// </summary>
        private long BeforeOffset;
        /// <summary>
        /// El estado del copiador.
        /// </summary>
        private CopyState state;
        /// <summary>
        /// Stream del archivo origen;
        /// </summary>
        private FileStream fsSource;
        /// <summary>
        /// Stream del archivo destino
        /// </summary>
        private FileStream fsDes;
        #endregion

        #region Propiedades
        /// <summary>
        /// Mide el progreso de la copia.
        /// </summary>
        public int Progress
        {
            get { return progress; }
            protected set
            {
                progress = value;
                OnPropertyChanged("Progress");
            }
        }

        /// <summary>
        /// La velocidad de la Copia (Bytes/seg).
        /// </summary>
        public long Speed
        {
            get { return speed; }
            protected set
            {
                speed = value;
                OnPropertyChanged("Speed");
            }
        }

        /// <summary>
        /// La carpeta de donde se encuentra el archivo origen
        /// </summary>
        public string FromFolder
        {
            get
            {
                return source.DirectoryName;
            }
        }

        /// <summary>
        /// El Nombre del fichero.
        /// </summary>
        public string FileFriendlyName
        {
            get
            {
                return source.Name;
            }
        }

        /// <summary>
        /// La carperta destino.
        /// </summary>
        public string DestinationFolder
        {
            get
            {
                return destination.FullName;
            }
        }

        /// <summary>
        /// El tiempo restante.
        /// </summary>
        public TimeSpan RemainingTime
        {
            get
            {
                return TimeRemainingCalculator.Calculate(this);
            }

        }

        /// <summary>
        /// Siempre retorna 1 en objetos de esta clase.
        /// </summary>
        public int FilesCount
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// Siempre retorna 1 en objetos de esta clase.
        /// </summary>
        public long TotalLength
        {
            get
            {
                return this.source.Length;
            }
        }

        /// <summary>
        /// Cuanto se ha copiado en bytes.
        /// </summary>
        public long BytesCopied
        {
            get { return Offset; }
        }

        /// <summary>
        /// Siempre retorna 1 en objetos de esta clase.
        /// </summary>
        public int CopiedFiles
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// El estado del copiador.
        /// </summary>
        public CopyState State
        {
            get { return state; }
            protected set
            {
                state = value;
                OnPropertyChanged("State");
            }
        }     
        #endregion

        #region Eventos
        /// <summary>
        /// Para cuando cambia una propiedad notificar a la UI.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Evento que indica que se termino de copiar el archivo.
        /// </summary>
        public event FileCopyCompletedEventHandler Completed;

        /// <summary>
        /// Cuando se actualizen los datos
        /// </summary>
        public event DataUpdatedEventHandler OnDataUpdated;

        public event ProgressChangedEventHandler ProgressChanged;
        #endregion

        #region Constructores
        /// <summary>
        /// Permite construir un objeto FileCopier.
        /// </summary>
        /// <param name="Source">Archivo a Copiar.</param>
        /// <param name="Destination">Carpeta a la que se va a copiar el archivo.</param>
        public FileCopier(FileInfo Source, DirectoryInfo Destination)
        {
            this.source = Source;
            this.destination = Destination;
            this.State = CopyState.NoStartedYet;
            InitializeWorker();
        }
        #endregion

        #region Metodos Publicos
        /// <summary>
        /// Empezar a copiar.
        /// </summary>
        public void StarCopy()
        {
            if (!worker.IsBusy)
            {
                worker.RunWorkerAsync();
                InitializateTimer();               
            }
        }

        /// <summary>
        /// ParaPoner Pausa
        /// </summary>
        public void Pause()
        {
            this.State = CopyState.Pasued;
            this.speedTimer.Stop();
            worker.CancelAsync();
        }

        public void Cancel()
        {
            if (worker.IsBusy)
            {
                this.State = CopyState.Canceled;
                this.speedTimer.Stop();
                worker.CancelAsync();
            }
            else
            {
                string DestinationFileName = destination.FullName + @"\" + source.Name;
                FileInfo Target = new FileInfo(DestinationFileName);
                DeleteFile(Target);
            }
                      
        }
        #endregion

        #region Private Methods
        /// <summary>
        ///Preparar el Background Worker para copiar
        /// </summary>
        private void InitializeWorker()
        {
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            // Enable support for cancellation
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += Copy;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
        }

        /// <summary>
        /// Preparar el Timer
        /// </summary>
        private void InitializateTimer()
        {
            speedTimer = new Timer();
            speedTimer.Interval = RefreshTime;
            speedTimer.Tick += speedTimer_Tick;
            speedTimer.Start();
        }

        /// <summary>
        /// Preparar los streams de copiar
        /// </summary>
        /// <param name="Target"></param>
        private void InitializateStreams(FileInfo Target)
        {
            fsSource = new FileStream(source.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize);
            if (!destination.Exists)
            {
                destination.Create();
            }
            fsDes = new FileStream(Target.FullName, FileMode.Create, FileAccess.Write, FileShare.None,BufferSize);            
            //fsSource.Seek(Offset, SeekOrigin.Begin);
            //fsDes.Seek(Offset, SeekOrigin.Begin);          
        }

        /// <summary>
        /// Borrar el fichero cuando se cancele.
        /// </summary>
        /// <param name="Target"></param>
        private void DeleteFile(FileInfo Target)
        {
            try
            {
                fsDes.Close();
                File.Delete(Target.FullName);
                fsSource.Close();
            }
            catch
            {
                
            }           
        }
        #endregion

        #region EventsHandlers
        /// <summary>
        /// Se ejecuta cuando se termina la copia.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled==true) return;
            if (speedTimer != null)
            {
                speedTimer.Stop();
                speedTimer.Dispose();
            }

            this.State = CopyState.Completed;
            OnCompleted();
        }

        /// <summary>
        /// Metodo que realiza la copia como tal.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>     
        private void Copy(object sender, DoWorkEventArgs e)
        {
            try
            {
                string DestinationFileName = destination.FullName + @"\" + source.Name;
                FileInfo Target = new FileInfo(DestinationFileName);

                /******Checkear*******/
                if (State == CopyState.NoStartedYet)
                {
                    if (Target.Exists)
                    {
                        this.State = CopyState.StandBy;
                        AlreadyExistException ex = new AlreadyExistException(this);
                        throw ex;
                        //YA existe el fichero
                    }

                    DriveInfo d = new DriveInfo(destination.Root.ToString());
                    if (d.TotalFreeSpace <= source.Length)
                    {
                        this.State = CopyState.StandBy;
                        FileToBigException ex = new FileToBigException(this,source.Length-d.TotalFreeSpace);
                        throw ex;
                        //no cabe
                    }
                    InitializateStreams(Target);
                }



                /******End Chequear******/


                byte[] buffer = new byte[BufferSize];
                int Reader;
                State = CopyState.Copying;
                while (Offset < source.Length && (Reader = fsSource.Read(buffer, 0, BufferSize)) > 0)
                {
                    fsDes.Write(buffer, 0, Reader);
                    Offset += Reader;
                    double p = ((double)Offset / (double)source.Length);
                    p *= 100;
                    worker.ReportProgress((int)p);

                    if (worker.CancellationPending)
                    {
                        // Set the Cancel property
                        if (State == CopyState.Canceled)
                        {
                            DeleteFile(Target);
                        }

                        e.Cancel = true;
                        return;
                    }

                    
                }
                worker.ReportProgress(100);
                fsDes.Flush();
                fsDes.Close();
                fsSource.Close();

            }
            catch (Exception ex)
            {
                throw ex;
            }       
        }

    

        /// <summary>
        /// Se ejecuta cuando cambia el progreso de la copia del fichero.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Progress = e.ProgressPercentage;
            OnProgressChanged(e);
        }

        /// <summary>
        /// Evento del timer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void speedTimer_Tick(object sender, EventArgs e)
        {
            long NewSpeed = Offset - BeforeOffset;
            BeforeOffset = Offset;
            if (this.Speed == 0)
            {
                this.Speed = NewSpeed;
            }
            else
            {
                Speed = ((Speed + (NewSpeed)) / 2);
            }
            OnPropertyChanged("RemainingTime");
            DataUpdatedEventArgs duea = new DataUpdatedEventArgs();
            OnSendDataUpdated();
        }
        #endregion

        #region LanzarEventos
        /// <summary>
        /// Cuando cambie una propiedad
        /// </summary>
        /// <param name="PropertyName"></param>
        private void OnPropertyChanged(string PropertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChangedEventArgs args = new PropertyChangedEventArgs(PropertyName);
                PropertyChanged(this, args);
            }
        }

        /// <summary>
        /// Cuando se completa la copias.
        /// </summary>
        private void OnCompleted()
        {
            
            if (Completed != null)
            {
                Completed(this, new FileCopyCompletedEventArgs());
            }
        }

        /// <summary>
        /// Lanzar OnDataUpdated.
        /// </summary>
        private void OnSendDataUpdated()
        {
            if (OnDataUpdated != null)
            {
                OnDataUpdated(this, new DataUpdatedEventArgs());
            }
        }

        private void OnProgressChanged(ProgressChangedEventArgs e)
        {
            if (ProgressChanged != null)
            {
                ProgressChanged(this,e);
            }
        }

        
        #endregion
    }
}
