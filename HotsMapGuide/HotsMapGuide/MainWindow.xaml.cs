using System;
using System.Collections.Generic;
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
using System.Data.SQLite;

// Used for timer
using System.Windows.Threading;

// Used for global hotkeys
using System.Runtime.InteropServices;
using System.Windows.Interop;



namespace HotsMapGuide
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variables

        // Constants
        public const int EVENT_INFO_COLUMN = 2;
        public const int TIMER_COLUMN = 1;
        public const double PROGRESS_BAR_SEGMENTS = 100d;
        public const int DEFAULT_STAGE = 1;
        public const int DEFAULT_COMBOBOX_VALUE = 0;
        
        // SQLite connection
        SQLiteConnection dataConnection;

        // Current Selection Variables
        public string selectedMap = "";
        public int currentStage = 1;
        public int rowCount = 0; // Number of rows in specified table

        // Timer
        public DispatcherTimer eventTimer;
        public int timeLeftTilEvent;
        public int timerStartTime;

        // Global Hotkey Variables
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        private const int HOTKEY_ID = 9000;
        private const uint MOD_NONE = 0x0000;
        private const uint VK_UP = 0x26;
        private const uint VK_RIGHT = 0x27;
        private const uint VK_LEFT = 0x25;

        #endregion


        // On Startup
        public MainWindow()
        {
            InitializeComponent();

            // Load comboBox data on startup
            PopulateComboBoxWithMapNames();

            // Set default drop down box value
            comboBox_MapSelector.SelectedIndex = DEFAULT_COMBOBOX_VALUE;
            SetMapAndProperties();
        }


        #region Global Hotkey Implementation

        private IntPtr _windowHandle;
        private HwndSource _source;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _windowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(HwndHook);

            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_NONE, VK_UP);
            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_NONE, VK_RIGHT);
            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_NONE, VK_LEFT);
        }

        /// <summary>
        /// Method where keypresses are handled
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        /// <returns></returns>
        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            int vkey = (((int)lParam >> 16) & 0xFFFF);
                            if (vkey == VK_UP) // Start Key
                            {
                                StartEvent();
                            }
                            if (vkey == VK_RIGHT) // Increment stage
                            {
                                IncrementStage();
                            }
                            if (vkey == VK_LEFT) // Decrement stage
                            {
                                DecrementStage();
                            }
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            _source.RemoveHook(HwndHook);
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            base.OnClosed(e);
        }

        #endregion


        #region UI Updates

        /// <summary>
        /// Update UI Elements based on currently selected map
        /// </summary>
        public void UpdateUIElements()
        {
            label_MapName.Content = selectedMap;
            UpdateEventInfoLabel();
        }


        /// <summary>
        /// Fill Map Selector ComboBox with Map Names
        /// </summary>
        public void PopulateComboBoxWithMapNames()
        {
            OpenDatabaseConnection();

            // Select names of all tables in database
            SQLiteDataReader dataReader =
                SendQueryAndReturnData("SELECT name FROM sqlite_master WHERE type='table'");

            while (dataReader.Read())
            {
                // Add table names to comboBox
                comboBox_MapSelector.Items.Add(dataReader.GetString(0));
            }

            CloseDatabaseConnection();
        }


        /// <summary>
        /// Updates Event Info label from database
        /// </summary>
        public void UpdateEventInfoLabel()
        {
            OpenDatabaseConnection();

            SQLiteDataReader dataReader =
                SendQueryAndReturnData("SELECT * FROM " + selectedMap + " WHERE ID=" + currentStage);

            while (dataReader.Read())
            {
                label_EventInfo.Content = dataReader.GetString(EVENT_INFO_COLUMN); 
            }

            CloseDatabaseConnection();
        }


        /// <summary>
        /// Sets the selected map variable
        /// </summary>
        public void SetSelectedMap()
        {
            // Set selected map from comboBox current selection
            selectedMap = comboBox_MapSelector.SelectedItem.ToString();
        }

        #endregion


        #region UI Events

        /// <summary>
        /// Called when comboBox is set/changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBox_MapSelector_DropDownClosed(object sender, EventArgs e)
        {
            SetMapAndProperties();
        }


        /// <summary>
        /// Sets selected map, stops timer, resets progress bar, Updates UI labels, sets internal variable for amount of rows
        /// </summary>
        public void SetMapAndProperties()
        {
            currentStage = DEFAULT_STAGE; // Reset stage
            SetSelectedMap();
            StopTimerIfRunning();
            ResetProgressBar();
            UpdateUIElements();
            SetAmtOfRows();
        }


        /* Needs to be removed eventually */
        /// <summary>
        /// Called when the Start button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_Start_Click(object sender, RoutedEventArgs e)
        {
            StartEvent();
        }


        /// <summary>
        /// Starts event and begins countdown
        /// </summary>
        private void StartEvent()
        {
            CreateTimerInstance();

            if (!eventTimer.IsEnabled)
            {
                OpenDatabaseConnection();

                SQLiteDataReader dataReader =
                    SendQueryAndReturnData("SELECT * FROM " + selectedMap + " WHERE ID=" + currentStage);

                while (dataReader.Read())
                {
                    timerStartTime = dataReader.GetInt32(TIMER_COLUMN);
                }

                CloseDatabaseConnection();

                InitializeTimer(timerStartTime);
            }
        }


        /// <summary>
        /// Advances to the next stage and resets timer
        /// </summary>
        private void IncrementStage()
        {
            StopTimerIfRunning();
            ResetProgressBar();

            if (currentStage < rowCount)
            {
                currentStage += 1;
                UpdateUIElements();
            }
        }


        /// <summary>
        /// Goes backwards by a stage and resets timer
        /// </summary>
        private void DecrementStage()
        {
            StopTimerIfRunning();
            ResetProgressBar();

            if (currentStage > 1)
            {
                currentStage -= 1;
                UpdateUIElements();
            }
        }


        /* Needs to be removed eventually if/when buttons are removed */
        /// <summary>
        /// Right arrow button for incrementing the current stage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Right_Click(object sender, RoutedEventArgs e)
        {
            IncrementStage();
        }

        /* Needs to be removed eventually if/when buttons are removed */
        /// <summary>
        /// Left arrow button for decrementing the current stage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Left_Click(object sender, RoutedEventArgs e)
        {
            DecrementStage();
        }

        #endregion


        #region Database Connection Methods

        public void OpenDatabaseConnection()
        {
            dataConnection = new SQLiteConnection(@"Data Source=HotsMapsDB.db;Version=3;");

            dataConnection.Open();
        }

        public void CloseDatabaseConnection()
        {
            dataConnection.Close();
        }

        /// <summary>
        /// Takes in and sends a SQL query then returns the Data Reader
        /// </summary>
        /// <param name="query">Query to the database</param>
        /// <returns></returns>
        public SQLiteDataReader SendQueryAndReturnData(string query)
        {
            SQLiteCommand createCommand = new SQLiteCommand(query, dataConnection);
            SQLiteDataReader dataReader = createCommand.ExecuteReader();
            return dataReader;
        }


        /// <summary>
        /// Set rowCount variable to number of rows in specified table
        /// </summary>
        public void SetAmtOfRows()
        {
            OpenDatabaseConnection();

            SQLiteDataReader dataReader =
                SendQueryAndReturnData("SELECT COUNT(*) FROM " + selectedMap); // Get amount of rows in specified table

            while (dataReader.Read())
            {
                rowCount = dataReader.GetInt32(0);
            }

            CloseDatabaseConnection();
        }

        #endregion


        #region Timer Methods

        public void CreateTimerInstance()
        {
            if (eventTimer != null)
            {
                StopTimerIfRunning();
                ResetProgressBar();
                eventTimer = null;
            }

            eventTimer = new DispatcherTimer();
        }


        /// <summary>
        /// Start timer and set amount of time to run
        /// </summary>
        /// <param name="timerStartValue">Time in seconds that the timer will count down</param>
        public void InitializeTimer(int initialTime)
        {
            timeLeftTilEvent = initialTime;

            eventTimer.Tick += new EventHandler(eventTimer_Tick);
            eventTimer.Interval = new TimeSpan(0, 0, 1); // Timer ticks every second
            
            eventTimer.Start();
        }

        /// <summary>
        /// Called for each Tick of the Timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void eventTimer_Tick(object sender, EventArgs e)
        {
            if (timeLeftTilEvent > 0)
            {
                label_TimeLeft.Content = ConvertSecsToMinutes(timeLeftTilEvent);
                timeLeftTilEvent -= 1;
                UpdateProgressBar();
            }
            else // What happens when timer ends
            {
                eventTimer.Stop();
            }
        }


        /// <summary>
        /// Converts seconds to minute:second format
        /// </summary>
        /// <param name="secs">Total number of seconds</param>
        /// <returns>String representing mm:ss</returns>
        public string ConvertSecsToMinutes(int secs)
        {
            TimeSpan time = TimeSpan.FromSeconds(secs);
            string convertedTime = time.ToString(@"mm\:ss");
            return convertedTime;
        }


        /// <summary>
        /// Checks if timer is running and stops it
        /// </summary>
        public void StopTimerIfRunning()
        {
            if (eventTimer != null)
            {
                if (eventTimer.IsEnabled)
                {
                    eventTimer.Stop();
                    label_TimeLeft.Content = 0;
                }
            }
        }


        /// <summary>
        /// Resets the progress bar back to 0
        /// </summary>
        public void ResetProgressBar()
        {
            progressBar.Value = 0d;
        }

        
        /// <summary>
        /// Updates progress bar based on timerStartTime
        /// </summary>
        public void UpdateProgressBar()
        {
            progressBar.Value += PROGRESS_BAR_SEGMENTS / timerStartTime;
        }


        #endregion


    }


}
