using ClassifiedPrint.Core.Models;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassifiedPrint.Core.Repository
{
    public class NewspaperRespository : INewspaperRespository
    {
        private readonly string connectionString;
        public NewspaperRespository(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("MBN.Newspaper");
        }

        internal NpgsqlConnection Connection
        {
            get
            {
                return new NpgsqlConnection(connectionString);
            }
        }

        public void Add(Newspaper item)
        {
            using (var dbConnection = Connection)
            {
                dbConnection.Open();
                dbConnection.Execute("INSERT INTO newspaper (ClassifiedId,ContractNo,BeginDate,EndDate,Content,Phone,Created,Col,Page) VALUES(@Id,@ContractNo,@BDate,@EDate,@Content,@Phone,@Created,@Col,@Page)", item);
            }
        }

        public void ExecuteBulkOperation(Command command)
        {
            using (var conn = Connection)
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;

                    if (command.Parameters is Dictionary<string, object> commandParameters)
                        foreach (var parameter in commandParameters)
                        {
                            cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
                        }

                    cmd.CommandText = command.Sql;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public async Task ExecuteBulkOperationAsync(Command command)
        {
            using (var conn = Connection)
            {
                await conn.OpenAsync();

                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;

                    if (command.Parameters is Dictionary<string, object> commandParameters)
                        foreach (var parameter in commandParameters)
                        {
                            cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
                        }

                    cmd.CommandText = command.Sql;
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public IEnumerable<Newspaper> FindAll()
        {
            using (IDbConnection dbConnection = Connection)
            {
                dbConnection.Open();
                return dbConnection.Query<Newspaper>("SELECT * FROM newspaper");
            }
        }

        public Newspaper FindByID(int id)
        {
            using (IDbConnection dbConnection = Connection)
            {
                dbConnection.Open();
                return dbConnection.Query<Newspaper>("SELECT * FROM newspaper WHERE ClassifiedId = @Id", new { Id = id }).FirstOrDefault();
            }
        }

        public void Remove(int id)
        {
            using (IDbConnection dbConnection = Connection)
            {
                dbConnection.Open();
                dbConnection.Execute("DELETE FROM newspaper WHERE ClassifiedId=@ClassifiedId", new { ClassifiedId = id });
            }
        }

        public void Update(Newspaper item)
        {
            using (IDbConnection dbConnection = Connection)
            {
                dbConnection.Open();                
                dbConnection.Query("UPDATE newspaper SET ContractNo = @ContractNo,  BeginDate  = @BeginDate, EndDate= @EndDate, Content= @Content, Phone=@Phone,Created=@Created,Col=@Col,Page=@Page WHERE ClassifiedId = @ClassifiedId", item);
            }
        }

    }
}
