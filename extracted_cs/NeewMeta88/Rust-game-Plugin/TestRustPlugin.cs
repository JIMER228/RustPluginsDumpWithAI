using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using Facepunch.Extend;
using MySql.Data.MySqlClient;
using Console = System.Console;


namespace Oxide.Plugins
{
    [Info("Test_Plugin", "EGORIK", "0.0.1")]
    public class TestRustPlugin : RustPlugin
    {
      #region Init

      void Init()
      {
        Server.Broadcast("HEY, I'M IN");
        // DBConnect gg = new DBConnect();
        // gg.Login();
        // gg.conexion();
      }

      #endregion

      #region DBConnect

        class DBConnect
        {
          private MySqlConnection connection;
          private string server;
          private string database;
          private string uid;
          private string password;
        
          //Constructor
          public DBConnect()
          {
            Initialize();
          }
        
          //Initialize values
          private void Initialize()
          {
            server = "37.230.228.84";
            database = "s3529_rust";
            uid = "u3529_4X261Xwdr7";
            password = "t3T98ej+h3.NSLW+c+uGESfV";
            string connectionString;
            connectionString = "SERVER=" + server + ";" + "DATABASE=" + 
                               database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";
        
            connection = new MySqlConnection(connectionString);
          }
        
          //open connection to database
          public bool OpenConnection()
          {
            try
            {
              connection.Open();
              return true;
            }
            catch (MySqlException ex)
            {
              //When handling errors, you can your application's response based 
              //on the error number.
              //The two most common error numbers when connecting are as follows:
              //0: Cannot connect to server.
              //1045: Invalid user name and/or password.
              switch (ex.Number)
              {
                case 0:
                  Console.WriteLine("Cannot connect to server.  Contact administrator");
                  break;
        
                case 1045:
                  Console.WriteLine("Invalid username/password, please try again");
                  break;
              }
              return false;
            }
          }
        
          //Close connection
          public bool CloseConnection()
          {
            try
            {
              connection.Close();
              return true;
            }
            catch (MySqlException ex)
            {
              Console.WriteLine(ex.Message);
              return false;
            }
          }
        
          //Insert statement
          public int Insert_steamid(string name, string steamid)
          {
            if (this.Count(steamid) < 1)
            {
              string query = "INSERT INTO players (`name`, `steamid`) VALUES('"+name+"', '"+steamid+"')";

              //open connection
              if (this.OpenConnection() == true)
              {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, connection);
        
                //Execute command
                cmd.ExecuteNonQuery();
        
                //close connection
                this.CloseConnection();

                return 1;
              }
            }
            else
            {
              return 0;
            }
            return 0;
          }
            
          public void Insert_auction(string resource, int amount, int price, string player_name, string steamid)
          {
            string query = "INSERT INTO auction (`resource`, `amount`, `price`, `player_name`, `steam_id`) VALUES('"+resource+"', '"+amount+"', '"+price+"', '"+player_name+"', '"+steamid+"')";

            //open connection
            if (this.OpenConnection() == true)
            {
              //create command and assign the query and connection from the constructor
              MySqlCommand cmd = new MySqlCommand(query, connection);
        
              //Execute command
              cmd.ExecuteNonQuery();
        
              //close connection
              this.CloseConnection();
                        
            }
                    
          }
            
            
          //Count statement
          public int Count(string steamid)
          {
            string query = "SELECT Count(*) FROM players WHERE steamid = '"+steamid+"'";
            int Count = -1;
        
            //Open Connection
            if (this.OpenConnection() == true)
            {
              //Create Mysql Command
              MySqlCommand cmd = new MySqlCommand(query, connection);
        
              //ExecuteScalar will return one value
              Count = int.Parse(cmd.ExecuteScalar()+"");
        
              //close Connection
              this.CloseConnection();
        
              return Count;
            }
            else
            {
              return Count;
            }
          }
            
        
          //Update statement
          public void Update()
          {
            string query = "UPDATE product SET prod_name='myau', prod_price=22 WHERE prod_id=31";
        
            //Open connection
            if (this.OpenConnection() == true)
            {
              //create mysql command
              MySqlCommand cmd = new MySqlCommand();
              //Assign the query using CommandText
              cmd.CommandText = query;
              //Assign the connection using Connection
              cmd.Connection = connection;
        
              //Execute query
              cmd.ExecuteNonQuery();
        
              //close connection
              this.CloseConnection();
            }
          }
        
          //Delete statement
          public void Delete()
          {
            string query = "DELETE FROM product WHERE prod_id=31";
        
            if (this.OpenConnection() == true)
            {
              MySqlCommand cmd = new MySqlCommand(query, connection);
              cmd.ExecuteNonQuery();
              this.CloseConnection();
            }
          }
        
          //Select statement
          public List <string> [] Select()
          {
            string query = "SELECT * FROM product";
        
            //Create a list to store the result
            List< string >[] list = new List< string >[3];
            list[0] = new List< string >();
            list[1] = new List< string >();
            list[2] = new List< string >();
        
            //Open connection
            if (this.OpenConnection() == true)
            {
              //Create Command
              MySqlCommand cmd = new MySqlCommand(query, connection);
              //Create a data reader and Execute the command
              MySqlDataReader dataReader = cmd.ExecuteReader();
        
              //Read the data and store them in the list
              while (dataReader.Read())
              {
                list[0].Add(dataReader["id"] + "");
                list[1].Add(dataReader["prod_name"] + "");
                list[2].Add(dataReader["prod_price"] + "");
              }
        
              //close Data Reader
              dataReader.Close();
        
              //close Connection
              this.CloseConnection();
        
              //return list to be displayed
              return list;
            }
            else
            {
              return list;
            }
          }
            
        
          // //Backup
          // public void Backup()
          // {
          //     try
          //     {
          //         DateTime Time = DateTime.Now;
          //         int year = Time.Year;
          //         int month = Time.Month;
          //         int day = Time.Day;
          //         int hour = Time.Hour;
          //         int minute = Time.Minute;
          //         int second = Time.Second;
          //         int millisecond = Time.Millisecond;
          //
          //         //Save file to C:\ with the current date as a filename
          //         string path;
          //         path = "C:\\MySqlBackup" + year + "-" + month + "-" + day + 
          //                "-" + hour + "-" + minute + "-" + second + "-" + millisecond + ".sql";
          //         StreamWriter file = new StreamWriter(path);
          //
          //
          //         ProcessStartInfo psi = new ProcessStartInfo();
          //         psi.FileName = "mysqldump";
          //         psi.RedirectStandardInput = false;
          //         psi.RedirectStandardOutput = true;
          //         psi.Arguments = string.Format(@"-u{0} -p{1} -h{2} {3}", 
          //             uid, password, server, database);
          //         psi.UseShellExecute = false;
          //
          //         Process process = Process.Start(psi);
          //
          //         string output;
          //         output = process.StandardOutput.ReadToEnd();
          //         file.WriteLine(output);
          //         process.WaitForExit();
          //         file.Close();
          //         process.Close();
          //     }
          //     catch (IOException ex)
          //     {
          //         Debug.WriteLine("Error , unable to backup!");
          //     }
          // }
        
          // //Restore
          // public void Restore()
          // {
          //     try
          //     {
          //         //Read file from C:\
          //         string path;
          //         path = "C:\\MySqlBackup.sql";
          //         StreamReader file = new StreamReader(path);
          //         string input = file.ReadToEnd();
          //         file.Close();
          //
          //         ProcessStartInfo psi = new ProcessStartInfo();
          //         psi.FileName = "mysql";
          //         psi.RedirectStandardInput = true;
          //         psi.RedirectStandardOutput = false;
          //         psi.Arguments = string.Format(@"-u{0} -p{1} -h{2} {3}", 
          //             uid, password, server, database);
          //         psi.UseShellExecute = false;
          //
          //
          //         Process process = Process.Start(psi);
          //         process.StandardInput.WriteLine(input);
          //         process.StandardInput.Close();
          //         process.WaitForExit();
          //         process.Close();
          //     }
          //     catch (IOException ex)
          //     {
          //         Console.WriteLine("Error , unable to Restore!");
          //     }
          // }
        }

        #endregion

      #region Chat reg

        [ChatCommand("reg")]
        void SelectFromDB(BasePlayer player, string command, string[] args)
        {
          ulong id = player.userID;
          string name = player.displayName;
          string steamid = id.ToString();
            
          DBConnect gg = new DBConnect();
          gg.Insert_steamid(name,steamid);
          rust.SendChatMessage(player,"<color=#ff0000>[REGISTRATION]</color>","SUCCESS",player.userID.ToString());

        }

        #endregion

      #region Chat lot

        [ChatCommand("lot")]
        void Add_lot(BasePlayer player, string command, string[] args, string res)
        {
          if (args.Length < 3)
          {
            rust.SendChatMessage(player,"<color=#ff0000>[AUCTION]</color>","Not enough arguments!\n Use /lot [resource] [amount] [price]",player.userID.ToString());
          }
          else
          {
            ulong id = player.userID;
            string player_name = player.displayName;
            string steamid = id.ToString();
            string resource = args[0];
            int amount = args[1].ToInt();
            int price = args[2].ToInt();
            DBConnect gg = new DBConnect();
            gg.Insert_auction(resource,amount,price,player_name,steamid);
            rust.SendChatMessage(player,"<color=#ff0000>[AUCTION]</color>","SUCCESS",player.userID.ToString());

          }
            
        }

        #endregion

      #region BackGround

                string Background = @"[
      {
        ""name"": ""BackGround"",
        ""parent"": ""Overlay"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.Image"",
            ""material"": """",
            ""color"": ""0.2352941 0.2352941 0.2352941 0.7372549""
          },
          {
            ""type"": ""NeedsCursor""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0.3 0.2"",
            ""anchormax"": ""0.7 0.8"",
            ""offsetmin"": ""-20 0"",
            ""offsetmax"": ""0 0""
          }
        ]
      },
      {
        ""name"": ""CloseBtn"",
        ""parent"": ""BackGround"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.Button"",
            ""close"": ""BackGround"",
            ""command"": ""Close"",
            ""color"": ""0.6352941 0.2588235 0.2588235 1"",
            ""imagetype"": ""Tiled""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0.95 0.95"",
            ""anchormax"": ""1 1"",
            ""offsetmin"": ""0 0"",
            ""offsetmax"": ""-4 -4""
          }
        ]
      },
    ]
                    ";

                #endregion
                
      #region GuiTemplate

                static string GuiTemplate = @"[
    {
        ""name"": ""GuiHolder"",
        ""parent"": ""BackGround"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.Image"",
            ""material"": """",
            ""color"": ""0.5529412 0.5529412 0.5529412 0.7411765""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0.02 0.08"",
            ""anchormax"": ""0.98 0.9"",
            ""offsetmin"": ""0 0"",
            ""offsetmax"": ""0 0""
          }
        ]
      },
      {
        ""name"": ""ResTypeSort1"",
        ""parent"": ""GuiHolder"",
        ""components"": [
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0.001305347 0.500941"",
            ""anchormax"": ""0.9986947 0.998118"",
            ""offsetmax"": ""0 0""
          }
        ]
      },
      {
        ""name"": ""ResTypeBox1"",
        ""parent"": ""ResTypeSort1"",
        ""components"": [
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0.02617527 0.07570595"",
            ""anchormax"": ""0.3246082 0.9242941"",
            ""offsetmax"": ""0 0""
          }
        ]
      },
      {
        ""name"": ""ResType1"",
        ""parent"": ""ResTypeBox1"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.Button"",
            ""sprite"": """",
            ""material"": """",
            ""command"": ""{ResType1_command}"",
            ""color"": ""1 1 1 0""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0.1 0.1"",
            ""anchormax"": ""0.9 0.9"",
            ""offsetmin"": ""0 10"",
            ""offsetmax"": ""0 10""
          }
        ]
      },
      {
        ""name"": ""ResType1Img"",
        ""parent"": ""ResType1"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.Image"",
            ""material"": """",
            ""color"": ""1 1 1 1"",
            ""png"": ""{ResType1Png}""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0 0"",
            ""anchormax"": ""1 1"",
            ""offsetmax"": ""0 0""
          }
        ]
      },
      {
        ""name"": ""ResTypeText1"",
        ""parent"": ""ResTypeBox1"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.Text"",
            ""text"": ""{ResType1_name}"",
            ""fontSize"": 18,
            ""align"": ""MiddleCenter"",
            ""color"": ""0.3843137 0 0 1""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0 0"",
            ""anchormax"": ""1 0.15"",
            ""offsetmax"": ""0 0""
          }
        ]
      },
      {
        ""name"": ""ResTypeBox2"",
        ""parent"": ""ResTypeSort1"",
        ""components"": [
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0.3507835 0.07570595"",
            ""anchormax"": ""0.6492165 0.9242941"",
            ""offsetmin"": ""0 0"",
            ""offsetmax"": ""0 0""
          }
        ]
      },
      {
        ""name"": ""ResType2"",
        ""parent"": ""ResTypeBox2"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.Button"",
            ""sprite"": """",
            ""material"": """",
            ""command"": ""{ResType2_command}"",
            ""color"": ""1 1 1 0""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0.1 0.1"",
            ""anchormax"": ""0.9 0.9"",
            ""offsetmin"": ""0 10"",
            ""offsetmax"": ""0 10""
          }
        ]
      },
      {
        ""name"": ""ResType2Img"",
        ""parent"": ""ResType2"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.RawImage"",
            ""material"": """",
            ""color"": ""1 1 1 1"",
            ""png"": ""{ResType2Png}""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0 0"",
            ""anchormax"": ""1 1"",
            ""offsetmin"": ""0 0"",
            ""offsetmax"": ""0 0""
          }
        ]
      },
      {
        ""name"": ""ResTypeText2"",
        ""parent"": ""ResTypeBox2"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.Text"",
            ""text"": ""{ResType2_name}"",
            ""fontSize"": 18,
            ""align"": ""MiddleCenter"",
            ""color"": ""0.3843137 0 0 1""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0 0"",
            ""anchormax"": ""1 0.15"",
            ""offsetmin"": ""0 0"",
            ""offsetmax"": ""0 0""
          }
        ]
      },
      {
        ""name"": ""ResTypeBox3"",
        ""parent"": ""ResTypeSort1"",
        ""components"": [
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0.6753918 0.07570595"",
            ""anchormax"": ""0.9738247 0.9242941"",
            ""offsetmin"": ""0 0"",
            ""offsetmax"": ""0 0""
          }
        ]
      },
      {
        ""name"": ""ResType3"",
        ""parent"": ""ResTypeBox3"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.Button"",
            ""sprite"": """",
            ""material"": """",
            ""command"": ""{ResType3_command}"",
            ""color"": ""1 1 1 0""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0.1 0.1"",
            ""anchormax"": ""0.9 0.9"",
            ""offsetmin"": ""0 10"",
            ""offsetmax"": ""0 10""
          }
        ]
      },
      {
        ""name"": ""ResType3Img"",
        ""parent"": ""ResType3"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.RawImage"",
            ""material"": """",
            ""color"": ""1 1 1 1"",
            ""png"": ""{ResType3Png}""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0 0"",
            ""anchormax"": ""1 1"",
            ""offsetmin"": ""0 0"",
            ""offsetmax"": ""0 0""
          }
        ]
      },
      {
        ""name"": ""ResTypeText3"",
        ""parent"": ""ResTypeBox3"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.Text"",
            ""text"": ""{ResType3_name}"",
            ""fontSize"": 18,
            ""align"": ""MiddleCenter"",
            ""color"": ""0.3843137 0 0 1""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0 0"",
            ""anchormax"": ""1 0.15"",
            ""offsetmin"": ""0 0"",
            ""offsetmax"": ""0 0""
          }
        ]
      },
      {
        ""name"": ""ResTypeSort2"",
        ""parent"": ""GuiHolder"",
        ""components"": [
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0.001305347 0.001881957"",
            ""anchormax"": ""0.9986947 0.499059"",
            ""offsetmin"": ""0 0"",
            ""offsetmax"": ""0 0""
          }
        ]
      },
      {
        ""name"": ""ResTypeBox4"",
        ""parent"": ""ResTypeSort2"",
        ""components"": [
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0.02617527 0.07570595"",
            ""anchormax"": ""0.3246082 0.9242941"",
            ""offsetmin"": ""0 0"",
            ""offsetmax"": ""0 0""
          }
        ]
      },
      {
        ""name"": ""ResType4"",
        ""parent"": ""ResTypeBox4"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.Button"",
            ""sprite"": """",
            ""material"": """",
            ""command"": ""{ResType4_command}"",
            ""color"": ""1 1 1 0""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0.1 0.1"",
            ""anchormax"": ""0.9 0.9"",
            ""offsetmin"": ""0 10"",
            ""offsetmax"": ""0 10""
          }
        ]
      },
      {
        ""name"": ""ResType4Img"",
        ""parent"": ""ResType4"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.RawImage"",
            ""material"": """",
            ""color"": ""1 1 1 1"",
            ""png"": ""{ResType4Png}""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0 0"",
            ""anchormax"": ""1 1"",
            ""offsetmin"": ""0 0"",
            ""offsetmax"": ""0 0""
          }
        ]
      },
      {
        ""name"": ""ResTypeText4"",
        ""parent"": ""ResTypeBox4"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.Text"",
            ""text"": ""{ResType4_name}"",
            ""fontSize"": 18,
            ""align"": ""MiddleCenter"",
            ""color"": ""0.3843137 0 0 1""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0 0"",
            ""anchormax"": ""1 0.15"",
            ""offsetmin"": ""0 0"",
            ""offsetmax"": ""0 0""
          }
        ]
      },
      {
        ""name"": ""ResTypeBox5"",
        ""parent"": ""ResTypeSort2"",
        ""components"": [
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0.3507835 0.07570595"",
            ""anchormax"": ""0.6492165 0.9242941"",
            ""offsetmin"": ""0 0"",
            ""offsetmax"": ""0 0""
          }
        ]
      },
      {
        ""name"": ""ResType5"",
        ""parent"": ""ResTypeBox5"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.Button"",
            ""sprite"": """",
            ""material"": """",
            ""command"": ""{ResType5_command}"",
            ""color"": ""1 1 1 0""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0.1 0.1"",
            ""anchormax"": ""0.9 0.9"",
            ""offsetmin"": ""0 10"",
            ""offsetmax"": ""0 10""
          }
        ]
      },
      {
        ""name"": ""ResType5Img"",
        ""parent"": ""ResType5"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.RawImage"",
            ""material"": """",
            ""color"": ""1 1 1 1"",
            ""png"": ""{ResType5Png}""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0 0"",
            ""anchormax"": ""1 1"",
            ""offsetmin"": ""0 0"",
            ""offsetmax"": ""0 0""
          }
        ]
      },
      {
        ""name"": ""ResTypeText5"",
        ""parent"": ""ResTypeBox5"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.Text"",
            ""text"": ""{ResType5_name}"",
            ""fontSize"": 18,
            ""align"": ""MiddleCenter"",
            ""color"": ""0.3843137 0 0 1""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0 0"",
            ""anchormax"": ""1 0.15"",
            ""offsetmin"": ""0 0"",
            ""offsetmax"": ""0 0""
          }
        ]
      },
      {
        ""name"": ""ResTypeBox6"",
        ""parent"": ""ResTypeSort2"",
        ""components"": [
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0.6753918 0.07570595"",
            ""anchormax"": ""0.9738247 0.9242941"",
            ""offsetmin"": ""0 0"",
            ""offsetmax"": ""0 0""
          }
        ]
      },
      {
        ""name"": ""ResType6"",
        ""parent"": ""ResTypeBox6"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.Button"",
            ""sprite"": """",
            ""material"": """",
            ""command"": ""{ResType6_command}"",
            ""color"": ""1 1 1 0""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0.1 0.1"",
            ""anchormax"": ""0.9 0.9"",
            ""offsetmin"": ""0 10"",
            ""offsetmax"": ""0 10""
          }
        ]
      },
      {
        ""name"": ""ResType6Img"",
        ""parent"": ""ResType6"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.RawImage"",
            ""material"": """",
            ""color"": ""1 1 1 1"",
            ""png"": ""{ResType6Png}""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0 0"",
            ""anchormax"": ""1 1"",
            ""offsetmin"": ""0 0"",
            ""offsetmax"": ""0 0""
          }
        ]
      },
      {
        ""name"": ""ResTypeText6"",
        ""parent"": ""ResTypeBox6"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.Text"",
            ""text"": ""{ResType6_name}"",
            ""fontSize"": 18,
            ""align"": ""MiddleCenter"",
            ""color"": ""0.3843137 0 0 1""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0 0"",
            ""anchormax"": ""1 0.15"",
            ""offsetmin"": ""0 0"",
            ""offsetmax"": ""0 0""
          }
        ]
      }
    ]
                    ";

                #endregion

      #region RightArrowTemplate

                static string RightArrowTemplate = @"[
           {
        ""name"": ""RightArrow"",
        ""parent"": ""BackGround"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.Button"",
            ""command"": ""{RightArrow_command}"",
            ""color"": ""1 1 1 0""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0.5 0.02"",
            ""anchormax"": ""0.55 0.07"",
            ""offsetmin"": ""4 0"",
            ""offsetmax"": ""4 0""
          }
        ]
      },
      {
        ""name"": ""RightArrowImg"",
        ""parent"": ""RightArrow"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.Image"",
            ""material"": """",
            ""color"": ""1 1 1 1""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0 0"",
            ""anchormax"": ""1 1"",
            ""offsetmax"": ""0 0""
          }
        ]
      }
            ]";

                #endregion

      #region LeftArrowTemplate

                static string LeftArrowTemplate = @"[
                {
        ""name"": ""LeftArrow"",
        ""parent"": ""BackGround"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.Button"",
            ""command"": ""{LeftArrow_command}"",
            ""color"": ""1 1 1 0""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0.45 0.02"",
            ""anchormax"": ""0.5 0.07"",
            ""offsetmin"": ""-4 0"",
            ""offsetmax"": ""-4 0""
          }
        ]
      },
      {
        ""name"": ""LeftArrowImg"",
        ""parent"": ""LeftArrow"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.Image"",
            ""material"": """",
            ""color"": ""1 1 1 1""
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0 0"",
            ""anchormax"": ""1 1"",
            ""offsetmin"": ""0 0"",
            ""offsetmax"": ""0 0""
          }
        ]
      }
    ]";

                #endregion

      #region BackBtnTemplate

                private static string BackBtnTemplate = @"[{
        ""name"": ""BackBtn"",
        ""parent"": ""BackGround"",
        ""components"": [
          {
            ""type"": ""UnityEngine.UI.Button"",
            ""color"": ""0.2588235 0.2588235 0.6352941 1"",
            ""imagetype"": ""Tiled"",
            ""command"": ""{BackBtn_command}"",
            
          },
          {
            ""type"": ""RectTransform"",
            ""anchormin"": ""0.01 0.95"",
            ""anchormax"": ""0.06 1"",
            ""offsetmin"": ""0 0"",
            ""offsetmax"": ""0 -4""
          }
        ]
      }]";

                #endregion

      #region MainPages

                public static string MainPage1 = GuiTemplate
                  .Replace("{ResType1_name}", "Оружие")
                  .Replace("{ResType1Png}", "4114146089") //Ak47
                  .Replace("{ResType1_command}", "LoadLobby 1")
                  
                  .Replace("{ResType2_name}", "Патроны")
                  .Replace("{ResType2Png}", "3425839418") //Ak47_Ammo
                  .Replace("{ResType2_command}", "LoadLobby 3")

                  .Replace("{ResType3_name}", "Одежда")
                  .Replace("{ResType3Png}", "2240699438") //Facemask
                  .Replace("{ResType3_command}", "LoadLobby 4")

                  .Replace("{ResType4_name}", "Инструменты")
                  .Replace("{ResType4Png}", "132116810") //Jagghamer

                  .Replace("{ResType5_name}", "Ресурсы")
                  .Replace("{ResType5Png}", "3445866038") //Sulfur_Raw

                  .Replace("{ResType6_name}", "Медикаменты")
                  .Replace("{ResType6Png}", "1648452271"); //MedBox

                public static string MainPage2 = GuiTemplate
                  .Replace("{ResType1_name}", "Объекты")
                  .Replace("{ResType1Png}", "2268278493") //Workbench_3lvl

                  .Replace("{ResType2_name}", "Конструкции")
                  .Replace("{ResType2Png}", "3262676589") //Gate_Stone

                  .Replace("{ResType3_name}", "Электричество")
                  .Replace("{ResType3Png}", "355358597") //SolarPanel

                  .Replace("{ResType4_name}", "Компоненты")
                  .Replace("{ResType4Png}", "74429858") //Microchips

                  .Replace("{ResType5_name}", "Машины")
                  .Replace("{ResType5Png}", "2994921714") //Furgon

                  .Replace("{ResType6_name}", "Еда")
                  .Replace("{ResType6Png}", "410948125"); //Pumpkin

                #endregion

      #region LobbyPage

                private static string LobbyPage1_1 = GuiTemplate
                  .Replace("{ResType1_name}", "Луки")
                  .Replace("{ResType1Png}", "4023679912") //Bow
                  .Replace("{ResType1_command}", "LoadPage 1 ItemPages1")

                  .Replace("{ResType2_name}", "Пистолеты")
                  .Replace("{ResType2Png}", "3047012428") //SemiPistol
                  .Replace("{ResType2_command}", "LoadPage 2 ItemPages1")

                  .Replace("{ResType3_name}", "Дробовики")
                  .Replace("{ResType3Png}", "3080008443") //DoubleShotgun
                  .Replace("{ResType3_command}", "LoadPage 3 ItemPages1")

                  .Replace("{ResType4_name}", "ПП")
                  .Replace("{ResType4Png}", "3308909977") //Tompson
                  .Replace("{ResType4_command}", "LoadPage 4 ItemPages1")

                  .Replace("{ResType5_name}", "Винтовки")
                  .Replace("{ResType5Png}", "407919626") //LR
                  .Replace("{ResType5_command}", "LoadPage 5 ItemPages1")

                  .Replace("{ResType6_name}", "Особое")
                  .Replace("{ResType6Png}", "4003318085") //Bazooka
                  .Replace("{ResType6_command}", "LoadPage 7 ItemPages1");

                private static string LobbyPage1_2 = GuiTemplate
                  .Replace("{ResType1_name}", "Ближний бой")
                  .Replace("{ResType1Png}", "2813463138") //Tesak
                  .Replace("{ResType1_command}", "LoadPage 8 ItemPages1")

                  .Replace("{ResType2_name}", "Обвесы")
                  .Replace("{ResType2Png}", "1474718794") //DuloBoost
                  .Replace("{ResType2_command}", "LoadPage 10 ItemPages1")

                  .Replace("{ResType3_name}", "Взрывное")
                  .Replace("{ResType3Png}", "917958385") //C4
                  .Replace("{ResType3_command}", "LoadPage 12 ItemPages1")

                  .Replace("{ResType4_name}", "")
                  .Replace("{ResType4Png}", "3346546241") //EmptySpace

                  .Replace("{ResType5_name}", "")
                  .Replace("{ResType5Png}", "3346546241") //EmptySpace

                  .Replace("{ResType6_name}", "")
                  .Replace("{ResType6Png}", "3346546241"); //EmptySpace

                private static string LobbyPage2 = GuiTemplate
                  .Replace("{ResType1_name}", "Стрелы")
                  .Replace("{ResType1Png}", "3459023716") //SpeedArrow
                  .Replace("{ResType1_command}", "LoadPage 1 ItemPages2")

                  .Replace("{ResType2_name}", "Пистолетные")
                  .Replace("{ResType2Png}", "411261278") //PistolAmmo
                  .Replace("{ResType2_command}", "LoadPage 2 ItemPages2")

                  .Replace("{ResType3_name}", "Дробь")
                  .Replace("{ResType3Png}", "3771974180") //ShotgunAmmo
                  .Replace("{ResType3_command}", "LoadPage 3 ItemPages2")

                  .Replace("{ResType4_name}", "5.56")
                  .Replace("{ResType4Png}", "3425839418") //556Ammo
                  .Replace("{ResType4_command}", "LoadPage 4 ItemPages2")

                  .Replace("{ResType5_name}", "Ракеты")
                  .Replace("{ResType5Png}", "73823499") //SpeedRocket
                  .Replace("{ResType5_command}", "LoadPage 5 ItemPages2")

                  .Replace("{ResType6_name}", "Особое")
                  .Replace("{ResType6Png}", "2938214673") //40mmDef
                  .Replace("{ResType6_command}", "LoadPage 6 ItemPages2");
                
                private static string LobbyPage3 = GuiTemplate
                  .Replace("{ResType1_name}", "Голова")
                  .Replace("{ResType1Png}", "2240699438") //Facemask
                  .Replace("{ResType1_command}", "LoadPage 1 ItemPages3")

                  .Replace("{ResType2_name}", "Руки")
                  .Replace("{ResType2Png}", "732560708") //TacticalGloves
                  .Replace("{ResType2_command}", "LoadPage 4 ItemPages3")

                  .Replace("{ResType3_name}", "Тело")
                  .Replace("{ResType3Png}", "4128108763") //MetalBody
                  .Replace("{ResType3_command}", "LoadPage 5 ItemPages3")

                  .Replace("{ResType4_name}", "Ноги")
                  .Replace("{ResType4Png}", "183959976") //RoadLeg
                  .Replace("{ResType4_command}", "LoadPage 8 ItemPages3")

                  .Replace("{ResType5_name}", "Обувь")
                  .Replace("{ResType5Png}", "3640694102") //Boots
                  .Replace("{ResType5_command}", "LoadPage 10 ItemPages3")

                  .Replace("{ResType6_name}", "Особое")
                  .Replace("{ResType6Png}", "715700250") //Antirad
                  .Replace("{ResType6_command}", "LoadPage 11 ItemPages3");

                #endregion

      #region ItemPages

                private static string ItemPage1_1 = GuiTemplate
                  .Replace("{ResType1_name}", "Охотничий")
                  .Replace("{ResType1Png}", "4023679912") //Bow

                  .Replace("{ResType2_name}", "Блочный")
                  .Replace("{ResType2Png}", "970271032") //Bow_MLG

                  .Replace("{ResType3_name}", "Арбалет")
                  .Replace("{ResType3Png}", "712321353") //Crossbow

                  .Replace("{ResType4_name}", "")
                  .Replace("{ResType4Png}", "3346546241") //EmptySpace

                  .Replace("{ResType5_name}", "")
                  .Replace("{ResType5Png}", "3346546241") //EmptySpace

                  .Replace("{ResType6_name}", "")
                  .Replace("{ResType6Png}", "3346546241"); //EmptySpace
                
                private static string ItemPage1_2 = GuiTemplate
                  .Replace("{ResType1_name}", "Ёка")
                  .Replace("{ResType1Png}", "2860702433") //Eoka

                  .Replace("{ResType2_name}", "Револьвер")
                  .Replace("{ResType2Png}", "2072697169") //Revolver

                  .Replace("{ResType3_name}", "Питон")
                  .Replace("{ResType3Png}", "1027659342") //Python

                  .Replace("{ResType4_name}", "Пешка")
                  .Replace("{ResType4Png}", "3047012428") //SemiPistol

                  .Replace("{ResType5_name}", "Беретта") 
                  .Replace("{ResType5Png}", "49301716")  //Beretta

                  .Replace("{ResType6_name}", "Прототип")
                  .Replace("{ResType6Png}", "3431895087"); //Prototype
                
                private static string ItemPage1_3 = GuiTemplate
                  .Replace("{ResType1_name}", "Пайпа")
                  .Replace("{ResType1Png}", "88773297") //PipeShotgun

                  .Replace("{ResType2_name}", "Двушка")
                  .Replace("{ResType2Png}", "3080008443") //DoubleShotgun

                  .Replace("{ResType3_name}", "Помпа")
                  .Replace("{ResType3Png}", "1428964059") //PompShotgun

                  .Replace("{ResType4_name}", "Спас")
                  .Replace("{ResType4Png}", "1265303419") //SpasShotgun

                  .Replace("{ResType5_name}", "") 
                  .Replace("{ResType5Png}", "3346546241")  //EmptySpace

                  .Replace("{ResType6_name}", "")
                  .Replace("{ResType6Png}", "3346546241"); //EmptySpace
                
                private static string ItemPage1_4 = GuiTemplate
                  .Replace("{ResType1_name}", "СМГ")
                  .Replace("{ResType1Png}", "2164336086") //SMG

                  .Replace("{ResType2_name}", "Томпсон")
                  .Replace("{ResType2Png}", "3308909977") //Tompson

                  .Replace("{ResType3_name}", "МП5")
                  .Replace("{ResType3Png}", "1407740909") //MP5

                  .Replace("{ResType4_name}", "")
                  .Replace("{ResType4Png}", "3346546241") //EmptySpace

                  .Replace("{ResType5_name}", "") 
                  .Replace("{ResType5Png}", "3346546241") //EmptySpace

                  .Replace("{ResType6_name}", "")
                  .Replace("{ResType6Png}", "3346546241"); //EmptySpace
                
                private static string ItemPage1_5 = GuiTemplate
                  .Replace("{ResType1_name}", "Берданка")
                  .Replace("{ResType1Png}", "950982457") //Berdanka

                  .Replace("{ResType2_name}", "М39")
                  .Replace("{ResType2Png}", "20695481") //M39

                  .Replace("{ResType3_name}", "Лрка")
                  .Replace("{ResType3Png}", "407919626") //LR

                  .Replace("{ResType4_name}", "Ak47")
                  .Replace("{ResType4Png}", "4114146089") //Ak47

                  .Replace("{ResType5_name}", "Болт") 
                  .Replace("{ResType5Png}", "3784334886")  //Bolt

                  .Replace("{ResType6_name}", "Лка")
                  .Replace("{ResType6Png}", "4181661246"); //L96
                
                private static string ItemPage1_6 = GuiTemplate
                  .Replace("{ResType1_name}", "Пулемет")
                  .Replace("{ResType1Png}", "2857567755") //MachineGun

                  .Replace("{ResType2_name}", "М249")
                  .Replace("{ResType2Png}", "597520897") //M249

                  .Replace("{ResType3_name}", "")
                  .Replace("{ResType3Png}", "3346546241") //EmptySpace

                  .Replace("{ResType4_name}", "")
                  .Replace("{ResType4Png}", "3346546241") //EmptySpace

                  .Replace("{ResType5_name}", "") 
                  .Replace("{ResType5Png}", "3346546241") //EmptySpace

                  .Replace("{ResType6_name}", "")
                  .Replace("{ResType6Png}", "3346546241"); //EmptySpace
                
                private static string ItemPage1_7 = GuiTemplate
                  .Replace("{ResType1_name}", "Гвоздемет")
                  .Replace("{ResType1Png}", "3302993243") //NailGun

                  .Replace("{ResType2_name}", "Огнемет")
                  .Replace("{ResType2Png}", "3350361348") //FireGun

                  .Replace("{ResType3_name}", "Базука")
                  .Replace("{ResType3Png}", "4003318085") //Bazooka

                  .Replace("{ResType4_name}", "Подводное Ружье")
                  .Replace("{ResType4Png}", "3677452051") //SharkGun

                  .Replace("{ResType5_name}", "") 
                  .Replace("{ResType5Png}", "3346546241") //EmptySpace

                  .Replace("{ResType6_name}", "")
                  .Replace("{ResType6Png}", "3346546241"); //EmptySpace
                
                private static string ItemPage1_8 = GuiTemplate
                  .Replace("{ResType1_name}", "Длинный меч")
                  .Replace("{ResType1Png}", "3040385809") //LongSword

                  .Replace("{ResType2_name}", "Булава")
                  .Replace("{ResType2Png}", "765567583") //Bulava

                  .Replace("{ResType3_name}", "Тесак")
                  .Replace("{ResType3Png}", "2813463138") //Tesak

                  .Replace("{ResType4_name}", "Самодельный Меч")
                  .Replace("{ResType4Png}", "557743225") //HandmadeSword

                  .Replace("{ResType5_name}", "Боевой нож") 
                  .Replace("{ResType5Png}", "226450286") //FightKnife

                  .Replace("{ResType6_name}", "Мачете")
                  .Replace("{ResType6Png}", "1267457207"); //Machete
                
                private static string ItemPage1_9 = GuiTemplate
                  .Replace("{ResType1_name}", "Весло")
                  .Replace("{ResType1Png}", "2877996510") //Veslo

                  .Replace("{ResType2_name}", "Каменное копье")
                  .Replace("{ResType2Png}", "3276831904") //StoneSpear

                  .Replace("{ResType3_name}", "Деревянное копье")
                  .Replace("{ResType3Png}", "712213032") //Bazooka

                  .Replace("{ResType4_name}", "Костяной нож")
                  .Replace("{ResType4Png}", "416872458") //BoneKnife

                  .Replace("{ResType5_name}", "Костяная дубина") 
                  .Replace("{ResType5Png}", "2255509459") //BoneDubina

                  .Replace("{ResType6_name}", "")
                  .Replace("{ResType6Png}", "3346546241"); //EmptySpace
                
                private static string ItemPage1_10 = GuiTemplate
                  .Replace("{ResType1_name}", "Прицел 16х")
                  .Replace("{ResType1Png}", "2856268209") //Scope16x

                  .Replace("{ResType2_name}", "Прицел 8х")
                  .Replace("{ResType2Png}", "2130837147") //Scope8x

                  .Replace("{ResType3_name}", "Голограф")
                  .Replace("{ResType3Png}", "1192665578") //ScopeHolo

                  .Replace("{ResType4_name}", "Самодельный прицел")
                  .Replace("{ResType4Png}", "738156464") //ScopeHandmade

                  .Replace("{ResType5_name}", "Фонарик") 
                  .Replace("{ResType5Png}", "1121381401") //Flashlight

                  .Replace("{ResType6_name}", "Расширенный магазин")
                  .Replace("{ResType6Png}", "3432703049"); //BigMagazine
                
                private static string ItemPage1_11 = GuiTemplate
                  .Replace("{ResType1_name}", "Дульный ускоритель")
                  .Replace("{ResType1Png}", "1474718794") //DuloBoost

                  .Replace("{ResType2_name}", "Дульный тормоз")
                  .Replace("{ResType2Png}", "1767066927") //DuloStop

                  .Replace("{ResType3_name}", "Лазер")
                  .Replace("{ResType3Png}", "895653187") //Laser

                  .Replace("{ResType4_name}", "Глушитель")
                  .Replace("{ResType4Png}", "3572669343") //Silencer

                  .Replace("{ResType5_name}", "Берст") 
                  .Replace("{ResType5Png}", "3298323276") //Burst

                  .Replace("{ResType6_name}", "")
                  .Replace("{ResType6Png}", "3346546241"); //EmptySpace
                
                private static string ItemPage1_12 = GuiTemplate
                  .Replace("{ResType1_name}", "С4")
                  .Replace("{ResType1Png}", "917958385") //C4

                  .Replace("{ResType2_name}", "Сачель")
                  .Replace("{ResType2Png}", "3960637180") //Sachel

                  .Replace("{ResType3_name}", "Бобовка")
                  .Replace("{ResType3Png}", "4248915686") //Bobovka

                  .Replace("{ResType4_name}", "Граната")
                  .Replace("{ResType4Png}", "2858593143") //Grenade

                  .Replace("{ResType5_name}", "Молотов") 
                  .Replace("{ResType5Png}", "2633461944") //Molotov

                  .Replace("{ResType6_name}", "Флешка")
                  .Replace("{ResType6Png}", "803920941"); //Flashbang
                
                private static string ItemPage2_1 = GuiTemplate
                  .Replace("{ResType1_name}", "Деревянная")
                  .Replace("{ResType1Png}", "4281468501") //WoodenArrow

                  .Replace("{ResType2_name}", "Скоростная")
                  .Replace("{ResType2Png}", "3459023716") //SpeedArrow

                  .Replace("{ResType3_name}", "Костяная")
                  .Replace("{ResType3Png}", "35855562") //BoneArrow

                  .Replace("{ResType4_name}", "Огненная")
                  .Replace("{ResType4Png}", "937741547") //FireArrow

                  .Replace("{ResType5_name}", "")
                  .Replace("{ResType5Png}", "3346546241") //EmptySpace

                  .Replace("{ResType6_name}", "")
                  .Replace("{ResType6Png}", "3346546241"); //EmptySpace
                
                private static string ItemPage2_2 = GuiTemplate
                  .Replace("{ResType1_name}", "Обычный")
                  .Replace("{ResType1Png}", "411261278") //PistolAmmo

                  .Replace("{ResType2_name}", "Скоростной")
                  .Replace("{ResType2Png}", "2391710684") //SpeedPistolAmmo

                  .Replace("{ResType3_name}", "Зажигатльный")
                  .Replace("{ResType3Png}", "802433945") //FirePistolAmmo

                  .Replace("{ResType4_name}", "")
                  .Replace("{ResType4Png}", "3346546241") //EmptySpace

                  .Replace("{ResType5_name}", "") 
                  .Replace("{ResType5Png}", "3346546241")  //EmptySpace

                  .Replace("{ResType6_name}", "")
                  .Replace("{ResType6Png}", "3346546241"); //EmptySpace
                
                private static string ItemPage2_3 = GuiTemplate
                  .Replace("{ResType1_name}", "Дробь")
                  .Replace("{ResType1Png}", "3771974180") //ShotgunAmmo

                  .Replace("{ResType2_name}", "Пуля")
                  .Replace("{ResType2Png}", "4155533293") //ShotgunSingleAmmo

                  .Replace("{ResType3_name}", "Зажигатльный")
                  .Replace("{ResType3Png}", "1972907777") //ShotgunFireAmmo

                  .Replace("{ResType4_name}", "Самодельный")
                  .Replace("{ResType4Png}", "1237204368") //ShotgunHandmade

                  .Replace("{ResType5_name}", "") 
                  .Replace("{ResType5Png}", "3346546241")  //EmptySpace

                  .Replace("{ResType6_name}", "")
                  .Replace("{ResType6Png}", "3346546241"); //EmptySpace
                
                private static string ItemPage2_4 = GuiTemplate
                  .Replace("{ResType1_name}", "Обычный")
                  .Replace("{ResType1Png}", "3425839418") //556Ammo

                  .Replace("{ResType2_name}", "Скоростной")
                  .Replace("{ResType2Png}", "1425280439") //556SpeedAmmo

                  .Replace("{ResType3_name}", "Зажигатльный")
                  .Replace("{ResType3Png}", "18514891") //556FireAmmo

                  .Replace("{ResType4_name}", "Разрывной")
                  .Replace("{ResType4Png}", "238832608") //556BoomAmmo

                  .Replace("{ResType5_name}", "") 
                  .Replace("{ResType5Png}", "3346546241") //EmptySpace

                  .Replace("{ResType6_name}", "")
                  .Replace("{ResType6Png}", "3346546241"); //EmptySpace
                
                private static string ItemPage2_5 = GuiTemplate
                  .Replace("{ResType1_name}", "Обычная")
                  .Replace("{ResType1Png}", "3245383333") //Rocket

                  .Replace("{ResType2_name}", "Скоростная")
                  .Replace("{ResType2Png}", "73823499") //SpeedRocket

                  .Replace("{ResType3_name}", "Зажигатльная")
                  .Replace("{ResType3Png}", "539173559") //FireRocket

                  .Replace("{ResType4_name}", "Дымовая")
                  .Replace("{ResType4Png}", "1136422843") //SmokeRocket

                  .Replace("{ResType5_name}", "РСЗО") 
                  .Replace("{ResType5Png}", "2402236750")  //RSZO

                  .Replace("{ResType6_name}", "Торпеда")
                  .Replace("{ResType6Png}", "4052862516"); //Torped

                private static string ItemPage2_6 = GuiTemplate
                  .Replace("{ResType1_name}", "Гвозди")
                  .Replace("{ResType1Png}", "3285906548") //Nails

                  .Replace("{ResType2_name}", "Гарпун")
                  .Replace("{ResType2Png}", "2713089656") //Garpun

                  .Replace("{ResType3_name}", "Зенитная  ракета")
                  .Replace("{ResType3Png}", "4188725392") //ZenitRocket

                  .Replace("{ResType4_name}", "40мм картечь")
                  .Replace("{ResType4Png}", "2938214673") //40mmDef

                  .Replace("{ResType5_name}", "40мм фугас")
                  .Replace("{ResType5Png}", "3840826270") //40mmFugas

                  .Replace("{ResType6_name}", "40мм дым")
                  .Replace("{ResType6Png}", "3374341962"); //40mmSmoke
                
                private static string ItemPage3_1 = GuiTemplate
                  .Replace("{ResType1_name}", "Котелок")
                  .Replace("{ResType1Png}", "3425451061") //mvkMask

                  .Replace("{ResType2_name}", "Железная маска")
                  .Replace("{ResType2Png}", "2240699438") //Facemask

                  .Replace("{ResType3_name}", "Шлем бунтаря")
                  .Replace("{ResType3Png}", "1688401894") //BuntarMask

                  .Replace("{ResType4_name}", "Кофейная банка")
                  .Replace("{ResType4Png}", "405897033") //CoffeMask

                  .Replace("{ResType5_name}", "Волчья шапка")
                  .Replace("{ResType5Png}", "66339205") //WolfMask

                  .Replace("{ResType6_name}", "Костяной шлем")
                  .Replace("{ResType6Png}", "2787293805"); //BoneMask
                
                private static string ItemPage3_2 = GuiTemplate
                  .Replace("{ResType1_name}", "Деревянный шлем")
                  .Replace("{ResType1Png}", "1414277159") //WoodenMask

                  .Replace("{ResType2_name}", "Шлем из ведра")
                  .Replace("{ResType2Png}", "1529816493") //BucketMask

                  .Replace("{ResType3_name}", "Шапка шахтера")
                  .Replace("{ResType3Png}", "2897035522") //MineMask

                  .Replace("{ResType4_name}", "Свеча")
                  .Replace("{ResType4Png}", "3627613272") //CandleMask

                  .Replace("{ResType5_name}", "Панама") 
                  .Replace("{ResType5Png}", "704310729")  //Panama

                  .Replace("{ResType6_name}", "Балаклава")
                  .Replace("{ResType6Png}", "3281500805"); //Balaclava
                
                private static string ItemPage3_3 = GuiTemplate
                  .Replace("{ResType1_name}", "Повязка")
                  .Replace("{ResType1Png}", "3468979644") //ClothBalaclava

                  .Replace("{ResType2_name}", "Кепка")
                  .Replace("{ResType2Png}", "2924679685") //Cap

                  .Replace("{ResType3_name}", "Шапка")
                  .Replace("{ResType3Png}", "3541069717") //Hat

                  .Replace("{ResType4_name}", "Бандана")
                  .Replace("{ResType4Png}", "1949079286") //Bandana

                  .Replace("{ResType5_name}", "") 
                  .Replace("{ResType5Png}", "3346546241")  //EmptySpace

                  .Replace("{ResType6_name}", "")
                  .Replace("{ResType6Png}", "3346546241"); //EmptySpace
                
                private static string ItemPage3_4 = GuiTemplate
                  .Replace("{ResType1_name}", "Тактические перчатки")
                  .Replace("{ResType1Png}", "732560708") //TacticalGloves

                  .Replace("{ResType2_name}", "Перчатки из знаков")
                  .Replace("{ResType2Png}", "3124956195") //RoadGloves

                  .Replace("{ResType3_name}", "Кожаные перчатки")
                  .Replace("{ResType3Png}", "786388628") //LetherGloves

                  .Replace("{ResType4_name}", "Перчатки из мешковины")
                  .Replace("{ResType4Png}", "758785667") //ClothGloves

                  .Replace("{ResType5_name}", "") 
                  .Replace("{ResType5Png}", "3346546241") //EmptySpace

                  .Replace("{ResType6_name}", "")
                  .Replace("{ResType6Png}", "3346546241"); //EmptySpace
                
                private static string ItemPage3_5 = GuiTemplate
                  .Replace("{ResType1_name}", "Титан")
                  .Replace("{ResType1Png}", "2259948535") //mvkBody

                  .Replace("{ResType2_name}", "Железный нагрудник")
                  .Replace("{ResType2Png}", "4128108763") //MetalBody

                  .Replace("{ResType3_name}", "Броня из знаков")
                  .Replace("{ResType3Png}", "1652893442") //RoadBody

                  .Replace("{ResType4_name}", "Деревянный нагрудник")
                  .Replace("{ResType4Png}", "3277938710") //WoodenBody

                  .Replace("{ResType5_name}", "Толстовка") 
                  .Replace("{ResType5Png}", "2702341900")  //Hoody

                  .Replace("{ResType6_name}", "Зимняя куртка")
                  .Replace("{ResType6Png}", "1447773738"); //WinterCoat
                
                private static string ItemPage3_6 = GuiTemplate
                  .Replace("{ResType1_name}", "Куртка")
                  .Replace("{ResType1Png}", "4224533089") //Coat

                  .Replace("{ResType2_name}", "Водолазка")
                  .Replace("{ResType2Png}", "1342220827") //Vodolazka

                  .Replace("{ResType3_name}", "Жилет")
                  .Replace("{ResType3Png}", "2985093345") //Jelette

                  .Replace("{ResType4_name}", "Пончо")
                  .Replace("{ResType4Png}", "2875900219") //Poncho

                  .Replace("{ResType5_name}", "Футболка") 
                  .Replace("{ResType5Png}", "3217343007") //TShirt

                  .Replace("{ResType6_name}", "Рубашка")
                  .Replace("{ResType6Png}", "3239691468"); //Shirt
                
                private static string ItemPage3_7 = GuiTemplate
                  .Replace("{ResType1_name}", "Рубаха")
                  .Replace("{ResType1Png}", "3477863899") //RawShirt

                  .Replace("{ResType2_name}", "")
                  .Replace("{ResType2Png}", "3346546241") //EmptySpace

                  .Replace("{ResType3_name}", "")
                  .Replace("{ResType3Png}", "3346546241") //EmptySpace

                  .Replace("{ResType4_name}", "")
                  .Replace("{ResType4Png}", "3346546241") //EmptySpace

                  .Replace("{ResType5_name}", "") 
                  .Replace("{ResType5Png}", "3346546241") //EmptySpace

                  .Replace("{ResType6_name}", "")
                  .Replace("{ResType6Png}", "3346546241"); //EmptySpace
                
                private static string ItemPage3_8 = GuiTemplate
                  .Replace("{ResType1_name}", "Поножи")
                  .Replace("{ResType1Png}", "3382076184") //mvkLeg

                  .Replace("{ResType2_name}", "Попка из знаков")
                  .Replace("{ResType2Png}", "183959976") //RoadLeg

                  .Replace("{ResType3_name}", "Деревянные щитки")
                  .Replace("{ResType3Png}", "3046468679") //WoodenLeg

                  .Replace("{ResType4_name}", "Штаны")
                  .Replace("{ResType4Png}", "3021655829") //Pants

                  .Replace("{ResType5_name}", "Штаны из ткани") 
                  .Replace("{ResType5Png}", "3606809985") //ClothPants

                  .Replace("{ResType6_name}", "Штаны из шкуры")
                  .Replace("{ResType6Png}", "4060428600"); //LeatherPants
                
                private static string ItemPage3_9 = GuiTemplate
                  .Replace("{ResType1_name}", "Шорты")
                  .Replace("{ResType1Png}", "3768810937") //Shorts

                  .Replace("{ResType2_name}", "Юбка")
                  .Replace("{ResType2Png}", "3555904888") //Skirt

                  .Replace("{ResType3_name}", "")
                  .Replace("{ResType3Png}", "3346546241") //EmptySpace

                  .Replace("{ResType4_name}", "")
                  .Replace("{ResType4Png}", "3346546241") //EmptySpace

                  .Replace("{ResType5_name}", "") 
                  .Replace("{ResType5Png}", "3346546241") //EmptySpace

                  .Replace("{ResType6_name}", "")
                  .Replace("{ResType6Png}", "3346546241"); //EmptySpace
                
                private static string ItemPage3_10 = GuiTemplate
                  .Replace("{ResType1_name}", "Ботинки")
                  .Replace("{ResType1Png}", "3640694102") //Boots

                  .Replace("{ResType2_name}", "Сапоги")
                  .Replace("{ResType2Png}", "2537295065") //LetherBoots

                  .Replace("{ResType3_name}", "Мешки на ноги")
                  .Replace("{ResType3Png}", "1880808499") //ClothBoots

                  .Replace("{ResType4_name}", "")
                  .Replace("{ResType4Png}", "3346546241") //EmptySpace

                  .Replace("{ResType5_name}", "") 
                  .Replace("{ResType5Png}", "3346546241") //EmptySpace

                  .Replace("{ResType6_name}", "")
                  .Replace("{ResType6Png}", "3346546241"); //EmptySpace
                
                private static string ItemPage3_11 = GuiTemplate
                  .Replace("{ResType1_name}", "Антирад")
                  .Replace("{ResType1Png}", "715700250") //Antirad

                  .Replace("{ResType2_name}", "Арктический костюм")
                  .Replace("{ResType2Png}", "119100683") //ArcticRad

                  .Replace("{ResType3_name}", "Костяная броня")
                  .Replace("{ResType3Png}", "178691019") //BoneBody

                  .Replace("{ResType4_name}", "Подводная маска")
                  .Replace("{ResType4Png}", "2843941855") //WaterMask

                  .Replace("{ResType5_name}", "Баллон с воздухом") 
                  .Replace("{ResType5Png}", "74350025") //GasBaloon

                  .Replace("{ResType6_name}", "Гидрокостюм")
                  .Replace("{ResType6Png}", "3583533542"); //HydroCostume
                
                private static string ItemPage3_12 = GuiTemplate
                  .Replace("{ResType1_name}", "Ласты")
                  .Replace("{ResType1Png}", "2875699090") //WaterBoots

                  .Replace("{ResType2_name}", "Конная броня дерево")
                  .Replace("{ResType2Png}", "3624255747") //HorseWooden

                  .Replace("{ResType3_name}", "Конная броня металл")
                  .Replace("{ResType3Png}", "3820238586") //HorseMetall

                  .Replace("{ResType4_name}", "Подковы")
                  .Replace("{ResType4Png}", "2329450733") //HorseBoots

                  .Replace("{ResType5_name}", "Седло") 
                  .Replace("{ResType5Png}", "843744790") //Saddle

                  .Replace("{ResType6_name}", "Сумка на лошадь")
                  .Replace("{ResType6Png}", "1248649563"); //HorseBag

                  

                #endregion

      #region Arrows

                private static string RightArrowMain = RightArrowTemplate
                  .Replace("{RightArrow_command}", "SwitchPage Right MainPages");

                private static string LeftArrowMain = LeftArrowTemplate
                  .Replace("{LeftArrow_command}", "SwitchPage Left MainPages");
                
                private static string RightArrowItem1_1 = RightArrowTemplate
                  .Replace("{RightArrow_command}", "SwitchPage Right ItemPages1_1Slide");

                private static string LeftArrowItem1_1 = LeftArrowTemplate
                  .Replace("{LeftArrow_command}", "SwitchPage Left ItemPages1_1Slide");
                
                private static string RightArrowItem1_2 = RightArrowTemplate
                  .Replace("{RightArrow_command}", "SwitchPage Right ItemPages1_2Slide");

                private static string LeftArrowItem1_2 = LeftArrowTemplate
                  .Replace("{LeftArrow_command}", "SwitchPage Left ItemPages1_2Slide");
                
                private static string RightArrowItem1_3 = RightArrowTemplate
                  .Replace("{RightArrow_command}", "SwitchPage Right ItemPages1_3Slide");

                private static string LeftArrowItem1_3 = LeftArrowTemplate
                  .Replace("{LeftArrow_command}", "SwitchPage Left ItemPages1_3Slide");
                
                private static string RightArrowLobby1 = RightArrowTemplate
                  .Replace("{RightArrow_command}", "SwitchPage Right LobbyPage1Slide");

                private static string LeftArrowLobby1 = LeftArrowTemplate
                  .Replace("{LeftArrow_command}", "SwitchPage Left LobbyPage1Slide");
                
                private static string RightArrowItem3_1 = RightArrowTemplate
                  .Replace("{RightArrow_command}", "SwitchPage Right ItemPages3_1Slide");
                
                private static string LeftArrowItem3_1 = LeftArrowTemplate
                  .Replace("{LeftArrow_command}", "SwitchPage Left ItemPages3_1Slide");
                
                private static string RightArrowItem3_2 = RightArrowTemplate
                  .Replace("{RightArrow_command}", "SwitchPage Right ItemPages3_2Slide");

                private static string LeftArrowItem3_2 = LeftArrowTemplate
                  .Replace("{LeftArrow_command}", "SwitchPage Left ItemPages3_2Slide");
                
                private static string RightArrowItem3_3 = RightArrowTemplate
                  .Replace("{RightArrow_command}", "SwitchPage Right ItemPages3_3Slide");

                private static string LeftArrowItem3_3 = LeftArrowTemplate
                  .Replace("{LeftArrow_command}", "SwitchPage Left ItemPages3_3Slide");
                
                private static string RightArrowItem3_4 = RightArrowTemplate
                  .Replace("{RightArrow_command}", "SwitchPage Right ItemPages3_4Slide");

                private static string LeftArrowItem3_4 = LeftArrowTemplate
                  .Replace("{LeftArrow_command}", "SwitchPage Left ItemPages3_4Slide");

                #endregion

      #region BackBtns

                private static string BackBtn = BackBtnTemplate
                  .Replace("{BackBtn_command}", "Back 0");
                
                private static string BackBtn1 = BackBtnTemplate
                  .Replace("{BackBtn_command}", "Back 1");
                
                private static string BackBtn2 = BackBtnTemplate
                  .Replace("{BackBtn_command}", "Back 2");
                
                private static string BackBtn3 = BackBtnTemplate
                  .Replace("{BackBtn_command}", "Back 3");

                #endregion

      #region Main string

                static string[] MainPages = { MainPage1, MainPage2 };

                #endregion

      #region Lobby string

                static string[] LobbyPage = { LobbyPage1_1, LobbyPage1_2, LobbyPage2, LobbyPage3 };
            
                static string[] LobbyPage1Slide = { LobbyPage1_1, LobbyPage1_2 };

                #endregion

      #region Item string

                static string[] ItemPages1 = { ItemPage1_1, ItemPage1_2, ItemPage1_3, ItemPage1_4, ItemPage1_5, ItemPage1_6, ItemPage1_7, ItemPage1_8, ItemPage1_9, ItemPage1_10, ItemPage1_11, ItemPage1_12 };
            
                static string[] ItemPages1_1Slide = { ItemPage1_5, ItemPage1_6 };
                static string[] ItemPages1_2Slide = { ItemPage1_8, ItemPage1_9 };
                static string[] ItemPages1_3Slide = { ItemPage1_10, ItemPage1_11 };
            
                static string[] ItemPages2 = { ItemPage2_1, ItemPage2_2, ItemPage2_3, ItemPage2_4, ItemPage2_5, ItemPage2_6 };
                
                static string[] ItemPages3 = { ItemPage3_1, ItemPage3_2, ItemPage3_3, ItemPage3_4, ItemPage3_5, ItemPage3_6, ItemPage3_7, ItemPage3_8, ItemPage3_9, ItemPage3_10, ItemPage3_11, ItemPage3_12};
                
                static string[] ItemPages3_1Slide = { ItemPage3_1, ItemPage3_2, ItemPage3_3};
                static string[] ItemPages3_2Slide = { ItemPage3_5, ItemPage3_6, ItemPage3_7 };
                static string[] ItemPages3_3Slide = { ItemPage3_8, ItemPage3_9 };
                static string[] ItemPages3_4Slide = { ItemPage3_11, ItemPage3_12 };


                #endregion

      #region Arrows string

                string[] rightArrows = { RightArrowMain, RightArrowItem1_1, RightArrowItem1_2, RightArrowItem1_3, RightArrowLobby1, RightArrowItem3_1, RightArrowItem3_2, RightArrowItem3_3, RightArrowItem3_4 };
                string[] leftArrows = { LeftArrowMain, LeftArrowItem1_1, LeftArrowItem1_2, LeftArrowItem1_3, LeftArrowLobby1, LeftArrowItem3_1, LeftArrowItem3_2, LeftArrowItem3_3, LeftArrowItem3_4 };

                #endregion

      #region Back string

                string[] BackBtns = { BackBtn, BackBtn1, BackBtn2, BackBtn3 };

                #endregion

      #region PageNum

      static int PageNum = 0;
      
      class  Pages
      {
        public int PageNumm { get; set; }

        public Pages()
        {
          PageNumm = 0; // Set the initial value for model
        }

      }
                 
      #endregion

      #region Test

      [ChatCommand("test")]
      void test(BasePlayer player, string command, string[] args)
      {
        Pages zz = new Pages();
        rust.SendChatMessage(player,"<color=#ff0000>[TEST]</color>","PageNumm = " + zz.PageNumm.ToString(),player.userID.ToString());
        zz.PageNumm = 10;
        rust.SendChatMessage(player,"<color=#ff0000>[TEST]</color>","PageNumm = " + zz.PageNumm.ToString(),player.userID.ToString());

      }

      #endregion

      #region Console Close

            [ConsoleCommand("Close")]
            void CloseBtn()
            {
              PageNum = 0;
            }

            #endregion

      #region Console LoadLobby

             [ConsoleCommand("LoadLobby")]
             void LoadLobby(ConsoleSystem.Arg args)
             {
               BasePlayer player = (BasePlayer)args.Connection.player;
               PageNum = args.Args[0].ToInt() - 1;
               // int SwitchNum;
               
               CuiHelper.DestroyUi(player, "GuiHolder");
               CuiHelper.DestroyUi(player, "RightArrow");
               CuiHelper.DestroyUi(player, "LeftArrow");

               if (PageNum == 0)
               {
                 CuiHelper.AddUi(player, LobbyPage[PageNum]);
                 CuiHelper.AddUi(player, BackBtns[0]);
                 CuiHelper.AddUi(player, rightArrows[4]);
               }
               else
               {
                 CuiHelper.AddUi(player, LobbyPage[PageNum]);
                 CuiHelper.AddUi(player, BackBtns[0]);
               }

               PageNum = 0;

             }

             #endregion

      #region Console LoadPage

             [ConsoleCommand("LoadPage")]
             void LoadPage(ConsoleSystem.Arg args)
             {
               BasePlayer player = (BasePlayer)args.Connection.player;
               var pageList = ItemPages1;
               PageNum = args.Args[0].ToInt() - 1;
               int SwitchNum;
               int BackNum;
               
               CuiHelper.DestroyUi(player, "GuiHolder");
               CuiHelper.DestroyUi(player, "RightArrow");
               CuiHelper.DestroyUi(player, "LeftArrow");
               CuiHelper.DestroyUi(player, "BackBtn");
               
               switch (args.Args[1])
               {
                 case "ItemPages1":
                   pageList = ItemPages1;
                   BackNum = 1;
                   
                   switch (PageNum)
                   {
                     case 4:
                       SwitchNum = 1;
                       
                       CuiHelper.AddUi(player, pageList[PageNum]);
                       CuiHelper.AddUi(player, BackBtns[BackNum]);
                       CuiHelper.AddUi(player, rightArrows[SwitchNum]);
                       break;
                     
                     case 7:
                       SwitchNum = 2;
                       
                       CuiHelper.AddUi(player, pageList[PageNum]);
                       CuiHelper.AddUi(player, BackBtns[BackNum]);
                       CuiHelper.AddUi(player, rightArrows[SwitchNum]);
                       break;
                     
                     case 9:
                       SwitchNum = 3;
                       
                       CuiHelper.AddUi(player, pageList[PageNum]);
                       CuiHelper.AddUi(player, BackBtns[BackNum]);
                       CuiHelper.AddUi(player, rightArrows[SwitchNum]);
                       break;
                     
                     default:
                       CuiHelper.AddUi(player, pageList[PageNum]);
                       CuiHelper.AddUi(player, BackBtns[BackNum]);
                       break;
                   }

                   break;
                 
                 case "ItemPages2":
                   pageList = ItemPages2;
                   BackNum = 2;

                   CuiHelper.AddUi(player, pageList[PageNum]);
                   CuiHelper.AddUi(player, BackBtns[BackNum]);
                   break;
                 
                 case "ItemPages3":
                   pageList = ItemPages3;
                   BackNum = 3;
                   
                   switch (PageNum)
                   {
                     case 0:
                       SwitchNum = 5;
                       
                       CuiHelper.AddUi(player, pageList[PageNum]);
                       CuiHelper.AddUi(player, BackBtns[BackNum]);
                       CuiHelper.AddUi(player, rightArrows[SwitchNum]);
                       break;
                     
                     case 4:
                       SwitchNum = 6;
                       
                       CuiHelper.AddUi(player, pageList[PageNum]);
                       CuiHelper.AddUi(player, BackBtns[BackNum]);
                       CuiHelper.AddUi(player, rightArrows[SwitchNum]);
                       break;
                     
                     case 7:
                       SwitchNum = 7;
                       
                       CuiHelper.AddUi(player, pageList[PageNum]);
                       CuiHelper.AddUi(player, BackBtns[BackNum]);
                       CuiHelper.AddUi(player, rightArrows[SwitchNum]);
                       break;
                     
                     case 10:
                       SwitchNum = 8;
                       
                       CuiHelper.AddUi(player, pageList[PageNum]);
                       CuiHelper.AddUi(player, BackBtns[BackNum]);
                       CuiHelper.AddUi(player, rightArrows[SwitchNum]);
                       break;
                     
                     default:
                       CuiHelper.AddUi(player, pageList[PageNum]);
                       CuiHelper.AddUi(player, BackBtns[BackNum]);
                       break;
                   }

                   
                   break;
               }

               
               PageNum = 0;
             }

             #endregion

      #region Console SwitchPage

             [ConsoleCommand("SwitchPage")]
             void SwitchPage(ConsoleSystem.Arg args)
             {
               BasePlayer player = (BasePlayer)args.Connection.player;
               var pageList = MainPages;
               int SwitchNum = 0;

               switch (args.Args[1])
               {
                 case "MainPages":
                   pageList = MainPages;
                   SwitchNum = 0;
                   break;
                 case "ItemPages1_1Slide":
                   pageList = ItemPages1_1Slide;
                   SwitchNum = 1;
                   break;
                 case "ItemPages1_2Slide":
                   pageList = ItemPages1_2Slide;
                   SwitchNum = 2;
                   break;
                 case "ItemPages1_3Slide":
                   pageList = ItemPages1_3Slide;
                   SwitchNum = 3;
                   break;
                 case "LobbyPage1Slide":
                   pageList = LobbyPage1Slide;
                   SwitchNum = 4;
                   break;  
                 case "ItemPages3_1Slide":
                   pageList = ItemPages3_1Slide;
                   SwitchNum = 5;
                   break;  
                 case "ItemPages3_2Slide":
                   pageList = ItemPages3_2Slide;
                   SwitchNum = 6;
                   break;  
                 case "ItemPages3_3Slide":
                   pageList = ItemPages3_3Slide;
                   SwitchNum = 7;
                   break;  
                 case "ItemPages3_4Slide":
                   pageList = ItemPages3_4Slide;
                   SwitchNum = 8;
                   break;  
                  
               }

               if (args.Args[0] == "Right")
               {
                 PageNum++;
               }
               else
               {
                 PageNum--;
               }
              

               CuiHelper.DestroyUi(player, "GuiHolder");
               CuiHelper.DestroyUi(player, "RightArrow");
               CuiHelper.DestroyUi(player, "LeftArrow");


               CuiHelper.AddUi(player, pageList[PageNum]);

               if (PageNum == 0)
               {
                 CuiHelper.AddUi(player, rightArrows[SwitchNum]);
               }
               else if (PageNum == pageList.Length-1)
               {
                 CuiHelper.AddUi(player, leftArrows[SwitchNum]);
               }
               else
               {
                 CuiHelper.AddUi(player, rightArrows[SwitchNum]);
                 CuiHelper.AddUi(player, leftArrows[SwitchNum]);
               }

             }

             #endregion

      #region Chat auc

            [ChatCommand("auc")]
            void auc(BasePlayer player, string command, string[] args)
            {
              CuiHelper.AddUi(player, Background);
              CuiHelper.AddUi(player, MainPage1);
              CuiHelper.AddUi(player, RightArrowMain);
            }

            #endregion

      #region Console Back

        [ConsoleCommand("Back")]
        void BackBtn_command(ConsoleSystem.Arg args)
        {
          BasePlayer player = (BasePlayer)args.Connection.player;
          int LobbyPageNum = args.Args[0].ToInt() - 1;
          PageNum = 0;
          
          CuiHelper.DestroyUi(player, "GuiHolder");
          CuiHelper.DestroyUi(player, "RightArrow");
          CuiHelper.DestroyUi(player, "LeftArrow");
          CuiHelper.DestroyUi(player, "BackBtn");
          
          switch (args.Args[0])
          {
            case "0":
            {
              string gui1 = MainPages[0];
              string gui2 = rightArrows[0];

              CuiHelper.AddUi(player, gui1);
              CuiHelper.AddUi(player, gui2);
              break;
            }
            case "1":
            {
              string gui1 = LobbyPage[LobbyPageNum];
              string gui2 = rightArrows[4];
              string gui3 = BackBtns[0];

              CuiHelper.AddUi(player, gui1);
              CuiHelper.AddUi(player, gui2);
              CuiHelper.AddUi(player, gui3);
              break;
            }
            default:
            {
              LobbyPageNum++;
              string gui1 = LobbyPage[LobbyPageNum];
              string gui2 = BackBtns[0];
            
              CuiHelper.AddUi(player, gui1);
              CuiHelper.AddUi(player, gui2);
              break;
            }
          }
          
        }

        #endregion
        
    }
}