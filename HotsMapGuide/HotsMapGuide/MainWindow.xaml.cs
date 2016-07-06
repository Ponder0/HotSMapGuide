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



namespace HotsMapGuide
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // SQLite connection
        SQLiteConnection dataConnection;


        public MainWindow()
        {
            InitializeComponent();

            // Set label text
            label_MapName.Content = "HotS Map Guide";
            label_EventInfo.Content = "Data to come...";

            // Load data on startup
            OpenDatabaseConnection();
            PopulateComboBoxWithMapNames();
            CloseDatabaseConnection();
        }

        

        /// <summary>
        /// Fill Map Selector ComboBox with Map Names
        /// </summary>
        public void PopulateComboBoxWithMapNames()
        {
            string query = "SELECT name FROM sqlite_master WHERE type='table'"; // Select names of all tables in database
            SQLiteCommand createCommand = new SQLiteCommand(query, dataConnection);
            SQLiteDataReader dataReader = createCommand.ExecuteReader();

            while (dataReader.Read())
            {
                // Add table names to comboBox
                comboBox_MapSelector.Items.Add(dataReader.GetString(0));
            }
        }



        /**************
         * Database Connection Helper Methods
         **************/

        public void OpenDatabaseConnection()
        {
            dataConnection = new SQLiteConnection(@"Data Source=C:\Users\LT\Source\Repos\HotSMapGuide\HotsMapGuide\HotsMapGuide\HotsMapsDB.db;Version=3;");
            dataConnection.Open();
        }

        public void CloseDatabaseConnection()
        {
            dataConnection.Close();
        }


    }

    
}
