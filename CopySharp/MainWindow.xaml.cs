using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using CopyCore;
using System.IO;

namespace CopySharp
{

    public partial class MainWindow : Window
    {
        #region Campos
        private string Source = @"D:\Entertainment\Music´n Videos\Bryan Adams";
        private string DestinationFolder = @"C:\folder";
        private CopyHandler Copier;
        private bool IsSorted;
        private SortField FieldSorted;
        private SortDirection Direction;

        private Grid act;
        private Polygon SelectedColumnPolygon;
        #endregion

        #region Constructores
        public MainWindow()
        {
            try
            {
                Rect bounds = Properties.Settings.Default.WindowPosition;
                this.Top = bounds.Top;
                this.Left = bounds.Left;
                // Restore the size only for a manually sized
                // window.
                if (this.SizeToContent == SizeToContent.Manual)
                {
                    this.Width = bounds.Width;
                    this.Height = bounds.Height;
                }

            }
            catch (Exception)
            {


            }


            InitializeComponent();
            IsSorted = false;
            SelectedColumnPolygon = (Polygon)this.FindResource("FromPoligon");
        }
        #endregion

        #region Metodos Privados
        private void StarCopy()
        {
            DirectoryInfo s = new DirectoryInfo(Source);
            DirectoryInfo d = new DirectoryInfo(DestinationFolder);
            Copier = new CopyHandler(s, d);
            this.DataContext = Copier;
            Copier.StarToCopy();
            
        }

      



        private void SwapSorting(SortField property, Button sender)
        {
            if (Copier == null) return;
            if (Copier.State == CopyState.Canceled || Copier.State == CopyState.Completed || Copier.State == CopyState.StandBy)
            {
                return;
            }

            if (!IsSorted || property != FieldSorted)
            {
                FieldSorted = property;
                Direction = SortDirection.Ascending;
                IsSorted = true;
            }
            else
            {
                if (Direction == SortDirection.Ascending)
                {
                    Direction = SortDirection.Descending;
                }
                else
                {
                    Direction = SortDirection.Ascending;
                }
            }
            Copier.SortBy(FieldSorted, Direction);
            if (act != null)
            {
                act.Children.Remove(SelectedColumnPolygon);
            }
            act = (Grid)sender.Content;
            act.Children.Add(SelectedColumnPolygon);
        }

        private List<FileCopier> GetSelectedItems()
        {
            List<FileCopier> SelectedItems = new List<FileCopier>();
            foreach (FileCopier item in lstFiles.SelectedItems)
            {
                SelectedItems.Add(item);
            }
            return SelectedItems;
        }
        #endregion

        #region Comandos
        #region Validar Comandos
        private void PlayCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (Copier == null)
            {
                e.CanExecute = false;
                return;
            }
            if (Copier.State == CopyState.Pasued)
            {
                e.CanExecute = true;
                return;
            }
            e.CanExecute = false;
        }

        private void PauseCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (Copier == null)
            {
                e.CanExecute = false;
                return;
            }
            if (Copier.State == CopyState.Copying)
            {
                e.CanExecute = true;
                return;
            }
            e.CanExecute = false;
        }

        private void StopCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            //if ()
            //{
            //    e.CanExecute = false;

            //}
            if (Copier == null || Copier.State == CopyState.Canceled || Copier.State == CopyState.Completed)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        private void DeleteCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (lstFiles.SelectedIndex != -1)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }
        #endregion

        #region Ejecutar Comandos
        private void PlayCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Copier.Play();
            CommandManager.InvalidateRequerySuggested();
        }

        private void PauseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Copier.Pause();
            CommandManager.InvalidateRequerySuggested();
        }

        private void StopCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Copier.Cancel();
            CommandManager.InvalidateRequerySuggested();
        }

        private void DeleteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            List<FileCopier> list = GetSelectedItems();
            foreach (FileCopier item in list)
            {
                Copier.Delete(item);
            }
            CommandManager.InvalidateRequerySuggested();

        }
        #endregion

        #endregion

        #region Sorting
        private void SortByFileName_Click(object sender, RoutedEventArgs e)
        {
            SwapSorting(SortField.FileName, (Button)sender);
        }

        private void SortBySize_Click(object sender, RoutedEventArgs e)
        {
            SwapSorting(SortField.Size, (Button)sender);
        }

        private void SortByFromFolder_Click(object sender, RoutedEventArgs e)
        {
            SwapSorting(SortField.FromFolder, (Button)sender);
        }

        private void SortByDestination_Click(object sender, RoutedEventArgs e)
        {
            SwapSorting(SortField.DestinationFolder, (Button)sender);
        }
        #endregion

        #region ModifyFilesQueue
        public void btnUp_Click(object sender, RoutedEventArgs e)
        {
            List<FileCopier> Selected = GetSelectedItems();
            foreach (FileCopier item in Selected)
            {
                Copier.UpOneLevel(item);
            }
            lstFiles.SelectedIndex--;
            lstFiles.ScrollIntoView(lstFiles.Items[lstFiles.SelectedIndex]);
        }

        private void btnUpAll_Click(object sender, RoutedEventArgs e)
        {
            List<FileCopier> Selected = GetSelectedItems();
            foreach (FileCopier item in Selected)
            {
                Copier.UpAllLevels(item);
            }
            lstFiles.SelectedIndex = 0;
            lstFiles.ScrollIntoView(lstFiles.Items[lstFiles.SelectedIndex]);
        }

        private void btnDown_Click(object sender, RoutedEventArgs e)
        {
            List<FileCopier> Selected = GetSelectedItems();
            foreach (FileCopier item in Selected)
            {
                Copier.DownOneLevel(item);
            }
            lstFiles.SelectedIndex++;
            lstFiles.ScrollIntoView(lstFiles.Items[lstFiles.SelectedIndex]);
        }

        private void btnDownAll_Click(object sender, RoutedEventArgs e)
        {
            List<FileCopier> Selected = GetSelectedItems();
            foreach (FileCopier item in Selected)
            {
                Copier.DownAllLevels(item);
            }
            lstFiles.SelectedIndex = lstFiles.Items.Count - 1;
            lstFiles.ScrollIntoView(lstFiles.Items[lstFiles.SelectedIndex]);
        }
        #endregion

        #region Botones Temporales
        private void ChooseSourceBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog op = new System.Windows.Forms.FolderBrowserDialog();
            if (op.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Source = op.SelectedPath;
            }
        }

        private void ChooseDestinationBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog op = new System.Windows.Forms.FolderBrowserDialog();
            if (op.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DestinationFolder = op.SelectedPath;
            }
        }

        private void StarToCopy_BtnClick(object sender, RoutedEventArgs e)
        {
            //DirectoryInfo d = new DirectoryInfo(@"C:\folder\b\c");
            ((Button)sender).IsEnabled = false;
            StarCopy();
            CommandManager.InvalidateRequerySuggested();

        }
        #endregion


      







    }


}
