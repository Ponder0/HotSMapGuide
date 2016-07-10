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

using System.Windows.Threading;



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
        /// Handles key down event and increments/decrements current stage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.OemPeriod) // Right button
            {
                StopTimerIfRunning();
                ResetProgressBar();

                if (currentStage < rowCount)
                {
                    currentStage += 1;
                    UpdateUIElements();
                }
            }
            else if (e.Key == Key.OemComma) // Left button
            {
                StopTimerIfRunning();
                ResetProgressBar();

                if (currentStage > 1)
                {
                    currentStage -= 1;
                    UpdateUIElements();
                }
            }
            else if (e.Key == Key.OemQuestion) // Start button
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
        }


        /* Needs to be removed eventually */
        /// <summary>
        /// Right arrow button for incrementing the current stage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Right_Click(object sender, RoutedEventArgs e)
        {
            StopTimerIfRunning();
            ResetProgressBar();

            if (currentStage < rowCount)
            {
                currentStage += 1;
                UpdateUIElements();
            }
        }

        /* Needs to be removed eventually */
        /// <summary>
        /// Left arrow button for decrementing the current stage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Left_Click(object sender, RoutedEventArgs e)
        {
            StopTimerIfRunning();
            ResetProgressBar();

            if (currentStage > 1)
            {
                currentStage -= 1;
                UpdateUIElements();
            }
        }

        #endregion


        #region Database Connection Methods

        public void OpenDatabaseConnection()
        {
            dataConnection = new SQLiteConnection(@"Data Source=C:\Users\LT\Source\Repos\HotSMapGuide\HotsMapGuide\HotsMapGuide\HotsMapsDB.db;Version=3;");
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
