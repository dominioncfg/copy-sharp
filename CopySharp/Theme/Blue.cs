using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CopySharp.Theme
{
    public partial class Blue : ResourceDictionary
    {
        public Blue()
        {
            InitializeComponent();

        }
        private void titleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Window win = (Window)
            ((FrameworkElement)sender).TemplatedParent;
            win.DragMove();
            e.Handled = false;
        }
        private void CloseButton_Click(object sender, MouseButtonEventArgs e)
        {
            Window win = (Window)
            ((FrameworkElement)sender).TemplatedParent;
            win.Close();
        }

        private void MinimizeButton_Click(object sender, MouseButtonEventArgs e)
        {
            Window win = (Window) ((FrameworkElement)sender).TemplatedParent;
            win.WindowState = WindowState.Minimized;
        }

        public void ImgLoad(object sender, EventArgs e)
        {
            Uri u = new Uri(Environment.CurrentDirectory + "/Theme/Imgs/bg");
            BitmapImage bmp = new BitmapImage(u);

            Border b = (Border)sender;
            ImageBrush brush = new ImageBrush(bmp);
            brush.TileMode = TileMode.Tile;
            brush.Stretch = Stretch.None;
            b.Background = brush;

        }

        bool isWiden = false;

        private void window_initiateWiden(object sender, MouseEventArgs e)
        {
            isWiden = true;
        }
        Rectangle rect;
        private void window_Widen(object sender, MouseEventArgs e)
        {
            Window  win = (Window)((FrameworkElement)sender).TemplatedParent;
             rect = (Rectangle)sender;
            //Rectangle rect = new Rectangle();
            if (isWiden)
            {
                rect.CaptureMouse();
                double newWidth = e.GetPosition(win).X + 5;
                if (newWidth > 0) win.Width = newWidth;
            }
            win.MouseLeftButtonUp += window_endWiden;
        }
        private void window_endWiden(object sender, MouseEventArgs e)
        {
            isWiden = false;
            try
            {               
                rect.ReleaseMouseCapture();
            }
            catch (Exception)
            {
                
               
            }
            // Make sure capture is released.
          
        }
    }
}
