using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace AspNetCoreFileProcessing.Repositories
{
    public class SqlRepository
    {
        private static readonly string _connectionString = "Server=192.168.99.100,1401;Database=Demo;User Id=sa;Password=<YourStrong!Passw0rd>;";

        public Stream GetBinaryValue(int id)
        {
            //using (SqlConnection connection = new SqlConnection(_connectionString))
            //{
            //    connection.Open();
            //    using (SqlCommand command = new SqlCommand("SELECT [bindata] FROM [Streams] WHERE [id]=@id", connection))
            //    {
            //        command.Parameters.AddWithValue("id", id);

            //        // The reader needs to be executed with the SequentialAccess behavior to enable network streaming  
            //        // Otherwise ReadAsync will buffer the entire BLOB into memory which can cause scalability issues or even OutOfMemoryExceptions  
            //        using (SqlDataReader reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
            //        {
            //            if (reader.Read() && !reader.IsDBNull(0))
            //            {
            //                return reader.GetStream(0);
            //            }
            //            else
            //            {
            //                throw new FileNotFoundException();
            //            }
            //        }
            //    }
            //}
            SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();
            SqlCommand command = new SqlCommand("SELECT [bindata] FROM [Streams] WHERE [id]=@id", connection);
            command.Parameters.AddWithValue("id", id);
            var reader = new SqlBlobReader(command);
            reader.GetData();
            return reader;
        }

        public void StreamBLOBToServer(string textData, Stream file)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("INSERT INTO [Streams] (textdata,bindata) VALUES (@textdata, @bindata)", conn))
                {
                    cmd.Parameters.AddWithValue("@textdata", textData);

                    // Add a parameter which uses the FileStream we just opened  
                    // Size is set to -1 to indicate "MAX"  
                    cmd.Parameters.Add("@bindata", SqlDbType.Binary, -1).Value = file;

                    // Send the data to the server  
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
