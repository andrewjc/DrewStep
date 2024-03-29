﻿using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static ConsoleApp.Program;

namespace ConsoleApp
{
    class Taskbar
    {
        private Window? window;
        private AppConfigManager.AppConfig config;
        private Hashtable taskbarItems;

        public Taskbar(AppConfigManager.AppConfig config)
        {
            this.config = config;
            this.window = null;
            this.taskbarItems = new Hashtable();
        }

        internal void Create()
        {
            window = new Window()
            {
                Title = "Taskbar",
                Width = SystemParameters.PrimaryScreenWidth,
                Height = 40,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = false,
                Background = System.Windows.Media.Brushes.Transparent,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize,
                Topmost = true
            };

            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = 0;
            window.Top = SystemParameters.PrimaryScreenHeight - window.Height;

            LinearGradientBrush taskbarGradient = new LinearGradientBrush();
            taskbarGradient.StartPoint = new Point(0, 0);
            taskbarGradient.EndPoint = new Point(0, 1);
            taskbarGradient.GradientStops.Add(new GradientStop(Colors.LightBlue, 0.0));
            taskbarGradient.GradientStops.Add(new GradientStop(Colors.Blue, 1.0));

            StackPanel panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Background = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0))
            };
            window.Content = panel;

            window.Show();

            MakeTaskbarChildOfDesktop(window);


            RefreshTaskList();

            WindowEventHookManager.Instance.OnWindowCreated += RefreshTaskList;

        }

        private void RefreshTaskList()
        {
            win32_api.EnumWindows(new win32_api.EnumWindowsProc((hWnd, lParam) =>
            {
                if (win32_api.IsWindowVisible(hWnd))
                {
                    const int nChars = 256;
                    StringBuilder classBuff = new StringBuilder(nChars);
                    if (win32_api.GetClassName(hWnd, classBuff, nChars) > 0)
                    {
                        string windowClass = classBuff.ToString();
                        if (config.BlacklistedClasses.Contains(windowClass))
                        {
                            return true; // Skip this window and continue enumeration
                        }
                    }

                    int length = win32_api.GetWindowTextLength(hWnd);
                    StringBuilder titleBuff = new StringBuilder(length + 1);
                    win32_api.GetWindowText(hWnd, titleBuff, titleBuff.Capacity);
                    string windowTitle = titleBuff.ToString();
                    if (!string.IsNullOrWhiteSpace(windowTitle) &&
                        !config.BlacklistedTitles.Contains(windowTitle, StringComparer.OrdinalIgnoreCase))
                    {
                        Console.WriteLine(windowTitle);
                        AddTaskbarItem(hWnd, windowTitle);
                    }
                }
                return true; // continue enumeration
            }), IntPtr.Zero);
        }

        private void RefreshTaskList(IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            Console.WriteLine("Automation event:" + hwnd + " time: " + DateTime.Now.ToString("g"));
            try
            {
                AutomationElement element = AutomationElement.FromHandle(hwnd);
                if (element == null)
                {
                    return;
                }

                if (element.Current.ControlType.LocalizedControlType == "window")
                {
                    AddTaskbarItem(hwnd, element.Current.Name);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void AddTaskbarItem(nint hWnd, String windowTitle)
        {
            if (!taskbarItems.ContainsKey(hWnd))
            {
                Button button = new Button
                {
                    Content = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            new Image
                            {
                                Source = GetIcon(hWnd),
                                Width = 16,
                                Height = 16,
                                Margin = new Thickness(2)
                            },
                            new TextBlock
                            {
                                Text = windowTitle,
                                TextTrimming = TextTrimming.CharacterEllipsis,
                                Margin = new Thickness(2) 
                            }
                        }
                    },
                    Tag = hWnd,
                    Style = getTaskBarButtonStyle(this.config)
                };
                button.Style = getTaskBarButtonStyle(this.config);
                button.Click += (sender, e) =>
                {
                    IntPtr hwnd = (IntPtr)((Button)sender).Tag;
                    win32_api.ShowWindow(hwnd, 9); // SW_RESTORE
                    win32_api.SetForegroundWindow(hwnd);
                };

                ((StackPanel)button.Content).Width = 200 - 20;

                // Add the button to the taskbar
                if (window.Content is StackPanel panel)
                {
                    panel.Children.Add(button);
                    taskbarItems.Add(hWnd, button);
                }
            }
            else
            {
                // TODO: Update taskbar item if it exists - eg title change/status etc
            }
        }

        private ImageSource GetIcon(IntPtr hWnd)
        {
            try
            {
                IntPtr hIcon = win32_api.SendMessage(hWnd, WM_GETICON, ICON_SMALL2, 0);
                if (hIcon == IntPtr.Zero)
                    hIcon = GetClassLongPtr(hWnd, GCL_HICONSM);
                if (hIcon == IntPtr.Zero)
                    hIcon = win32_api.SendMessage(hWnd, WM_GETICON, ICON_SMALL, 0);
                if (hIcon == IntPtr.Zero)
                    hIcon = win32_api.SendMessage(hWnd, WM_GETICON, ICON_BIG, 0);
                if (hIcon == IntPtr.Zero)
                    hIcon = GetClassLongPtr(hWnd, GCL_HICON);

                // If an icon was found, convert it to an ImageSource
                if (hIcon != IntPtr.Zero)
                {
                    return Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
            }
            catch (Exception ex)
            {
                
            }
            return null;
        }

        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;
        private const int ICON_SMALL2 = 2;
        private const int WM_GETICON = 0x7F;
        private const int GCL_HICONSM = -34;
        private const int GCL_HICON = -14;

        public static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return win32_api.GetClassLongPtr64(hWnd, nIndex);
            else
                return new IntPtr(win32_api.GetClassLongPtr32(hWnd, nIndex));
        }


        private Style getTaskBarButtonStyle(AppConfigManager.AppConfig config)
        {
            Style buttonStyle = new Style(typeof(Button));

            LinearGradientBrush buttonBackground = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromRgb(225, 238, 255), 0.0),
                    new GradientStop(Color.FromRgb(192, 220, 255), 0.5),
                    new GradientStop(Color.FromRgb(10, 73, 158), 1.0)
                }
            };

            buttonStyle.Setters.Add(new Setter(Button.MarginProperty, new Thickness(2)));
            buttonStyle.Setters.Add(new Setter(Button.HeightProperty, 30.0));
            buttonStyle.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));
            buttonStyle.Setters.Add(new Setter(Button.BackgroundProperty, Brushes.Transparent));
            buttonStyle.Setters.Add(new Setter(Button.MaxWidthProperty, 200.0));

            ControlTemplate template = new ControlTemplate(typeof(Button));

            var border = new FrameworkElementFactory(typeof(Border));
            border.Name = "border";
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
            border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            border.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.MarginProperty, new Thickness(4));
            contentPresenter.SetValue(FrameworkElement.WidthProperty, 200.0);
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            contentPresenter.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.WordEllipsis);

            border.AppendChild(contentPresenter);

            template.VisualTree = border;

            template.Triggers.Add(new Trigger
            {
                Property = UIElement.IsMouseOverProperty,
                Value = true,
                Setters =
                {
                    new Setter(Button.BackgroundProperty, buttonBackground, "border")
                }
            });

            buttonStyle.Setters.Add(new Setter(Button.TemplateProperty, template));

            return buttonStyle;
        }

        private void MakeTaskbarChildOfDesktop(Window window)
        {
            WindowInteropHelper helper = new WindowInteropHelper(window);
            IntPtr hwnd = helper.Handle;

            IntPtr progman = win32_api.FindWindow("Progman", null);

            win32_api.SetParent(hwnd, progman);
        }

        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        const uint EVENT_OBJECT_CREATE = 0x8000;
        const uint WINEVENT_OUTOFCONTEXT = 0;

        public void Destroy()
        {
            if (window != null)
            {
                window.Close();
                window = null;
            }
        }
    }
}
