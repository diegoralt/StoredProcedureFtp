using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace StoredProcedureFtp
{
    class Program
    {
        static void Main(string[] args)
        {
            //definimos la cadena de conexión de nuestra base de datos SQL Server
            string connString = "Data Source=tcp:<nombredeServidor>.database.windows.net,1433;Initial Catalog=<nombreBasedeDatos>;Integrated Security=False;User ID=<usuario>@<nombredeServidor>;Password=<contraseña>;Connect Timeout=30";
            //declaramos variable de conexión
            SqlConnection conn = new SqlConnection(connString);
            //declaramos variable que ejecutara el procedimiento
            SqlCommand command = new SqlCommand("<Nombre de procedimiento almacenado>", conn);
            //definimos que la variable sera de tipo procedimiento almacenado
            command.CommandType = CommandType.StoredProcedure;

            //esta variable nos ayudara a obtener el resultado de la consulta que realiza el procedimiento
            SqlDataReader reader;
            //definimos una lista de nuestro modelo de datos que recibiremos
            List<ReportsModel> reports = new List<ReportsModel>();
            try
            {
                //abrimos la conexion a la base de datos
                conn.Open();
                //ejecutamos el stored y obtenemos los datos de la consulta
                reader = command.ExecuteReader();
                //mientras se tengan datos los agregamos a nuestro modelo
                while (reader.Read())
                {
                    reports.Add(new ReportsModel
                    {
                        CardNumber = Convert.ToString(reader["CardNumber"]),
                        Item = Convert.ToString(reader["Item"]),
                        Units = Convert.ToString(reader["Units"]),
                        UnitsBonus = Convert.ToString(reader["UnitsBonus"]),
                    });
                }
            }
            catch (Exception ex)
            {
                //imprimimos en consola el error
                Console.WriteLine(ex.Message);
            }
            finally
            {
                //cerramos conexion a la base de datos
                conn.Close();
            }

            //definimos una lista donde separaremos nuestro datos con "|"
            List<string> file = new List<string>();
            foreach (var item in reports)
            {
                file.Add(item.CardNumber + "|"  + item.Item + "|" + item.Units + "|" + item.UnitsBonus);
            }

            //llamamos a createFile para publicar el archivo en el servidor ftp
            createFile(file);

            Console.ReadLine();
        }

        public static void createFile(List<string> file)
        {
            //el archivo que publicaremos tendra la siguiente nomenclatura ddmmaaaa.txt
            //
            //declaramos una variable string para obtener la fecha sin el separador "/"
            string date = DateTime.Now.ToString("dd/MM/yyyy").Replace("/", "");
            
            //definimos la cadena de conexion al servidor ftp agregando la fecha y la extension del archivo
            //ej. ftp://miservidor.com/ddmmaaaa.txt
            string connftp = "<Direccion de servidor ftp>" + date + ".txt";

            //declaramos la variable request para hacer la conexion al servidor
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(connftp);
            //agregamos las credenciales del servidor
            request.Credentials = new NetworkCredential("<Usuario>", "<Contraseña>");
            //definimos que cargaremos un archivo al servidor
            request.Method = WebRequestMethods.Ftp.UploadFile;

            //convertimos nuestra lista de datos en un arreglo de bytes agregandole un salto de linea
            byte[] fileContent = file.SelectMany(s => Encoding.UTF8.GetBytes(s + Environment.NewLine)).ToArray();
            //obtenemos el tamaño del arreglo de bytes
            request.ContentLength = fileContent.Length;

            //definimos la variable requestStream que escribira los datos en el servidor
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(fileContent, 0, fileContent.Length);
            requestStream.Close();

            //declaramos la variable response para obtener la respuesta del servidor
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            //imprimimos en consola el resultado de subir el archivo
            Console.WriteLine(response.StatusDescription);

            response.Close();
        }
    }

    //creamos nuestro modelo de datos de la consulta
    class ReportsModel
    {
        public string CardNumber { get; set; }
        public string Item { get; set; }
        public string Units { get; set; }
        public string UnitsBonus { get; set; }
    }
}
