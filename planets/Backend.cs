using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace planets
{
    using System.IO;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Data.Sqlite;
    using System.Dynamic;


    namespace Planets
    {
        internal static class Backend
        {
            private static string defaultConnectionString = "Data Source=planetDatabase.db";
            private static SqliteConnection defaultDB = new SqliteConnection(defaultConnectionString);
            internal static string DefaultConnectionString
            {
                get => defaultConnectionString;
                set => defaultConnectionString = value;
                
            }
            /// <summary>
            /// initialize the db if it doesn't exist
            /// </summary>
            public static void CreateDefaultDB()
            {
                if (!File.Exists("planetDatabase.db"))
                {
                    using (var connection = new SqliteConnection(defaultConnectionString))
                    {
                        connection.Open();
                        var command = connection.CreateCommand();
                        command.CommandText = $@"
                            CREATE TABLE planets (
                                id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                name TEXT 
                            );
                    
                            CREATE TABLE properties(
                                name TEXT NOT NULL PRIMARY KEY,
                                type TEXT NOT NULL
                            );
                    
                            INSERT INTO properties (name, type)
                                VALUES ('id', 'System.Int32'),
                                       ('name', 'System.String');

                            INSERT INTO planets (id, name)
                                VALUES (NULL, 'Earth'),
                                       (NULL, 'Jupiter');
                        ";
                        command.ExecuteNonQuery();
                    }
                }
            }
            /// <summary>
            /// create a relational table for a newly added property
            /// </summary>
            /// <param name="dbConnectionString"></param>
            /// <param name="propertyName"></param>
            /// <param name="propertyType"></param>
            public static void AddProperty(string? dbConnectionString, string propertyName, Type propertyType)
            {
                using (var connection = new SqliteConnection(dbConnectionString ?? defaultConnectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    //var dt = (propertyType == typeof(bool)) ? "INTEGER" : "TEXT";
                    command.CommandText = $@"
                        CREATE TABLE {propertyName}_table (
                            id INTEGER NOT NULL PRIMARY KEY,
                            {propertyName} TEXT,
                            FOREIGN KEY (id) references planets(id) ON DELETE CASCADE ON UPDATE CASCADE
                        );                      

                        INSERT INTO properties (name, type)
                        VALUES ('{propertyName}', '{propertyType}');
                    ";
                    command.ExecuteNonQuery();
                }
            }
            /// <summary>
            /// delete a relational table of a property
            /// </summary>
            /// <param name="dbConnectionString"></param>
            /// <param name="propertyName"></param>
            public static void DeletePropertyTable(string? dbConnectionString, string propertyName)
            {
                using (var connection = new SqliteConnection(dbConnectionString ?? defaultConnectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = $@"
                        DROP TABLE {propertyName}_table;
                    ";
                    command.ExecuteNonQuery();
                }
            }
            /// <summary>
            /// update a single property of a single record
            /// </summary>
            /// <param name="dbConnectionString"></param>
            /// <param name="id"></param>
            /// <param name="propertyName"></param>
            /// <param name="propertyValue"></param>
            /// <param name="propertyType"></param>
            public static void Update(string? dbConnectionString, int id, string propertyName, string propertyValue, Type propertyType)
            {
                using (var connection = new SqliteConnection(dbConnectionString ?? defaultConnectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    if (propertyName=="name")
                        command.CommandText = $@"
                            INSERT OR REPLACE INTO planets(id,name)
                            Values ({id},'{propertyValue}') 
                        ";
                    else 
                    {
                        command.CommandText = $@"
                            INSERT OR REPLACE INTO {propertyName}_table(id,{propertyName})
                            Values ({id},'{propertyValue}') 
                        ";
                    }
                    
                    var res = command.ExecuteNonQuery();
                    if (res != 1)
                    {
                        Console.WriteLine();
                    }   
                }
            }
            /// <summary>
            /// add a new record
            /// </summary>
            /// <param name="dbConnectionString"></param>
            /// <param name="id"></param>
            public static void Insert_new(string? dbConnectionString, string id)
            {
                using (var connection = new SqliteConnection(dbConnectionString ?? defaultConnectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = $@"
                        INSERT INTO planets (id)
                        VALUES ({id})
                    ";
                    command.ExecuteNonQuery();
                }
            }
            /// <summary>
            /// delete a record
            /// </summary>
            /// <param name="dbConnectionString"></param>
            /// <param name="id"></param>
            public static void Delete(string? dbConnectionString, int id)
            {
                using (var connection = new SqliteConnection(dbConnectionString ?? defaultConnectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = $@"
                        DELETE FROM planets 
                        WHERE id = {id}
                    ";
                    command.ExecuteNonQuery();
                }
            }
            /// <summary>
            /// Read the entire database 
            /// </summary>
            /// <param name="dbConnectionString"></param>
            /// <param name="Items"></param>
            /// <param name="properties"></param>
            /// <exception cref="NotImplementedException"></exception>
            public static void Read(string? dbConnectionString, out List<dynamic> Items, out List<PlanetProperty> properties)
            {
                using (var connection = new SqliteConnection(dbConnectionString ?? defaultConnectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = @"SELECT * FROM properties";
                    properties = new List<PlanetProperty>();
                       
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string name = reader.GetString(0);
                            string type = reader.GetString(1);
                            var tp = Type.GetType(type);
                            properties.Add(new(name, tp));
                        }
                    }
                    var propTypes = properties.Select(p => p.type).ToList();
                    var sb = new StringBuilder();
                    sb.Append($@"
                        SELECT planets.id, planets.name 
                    ");
                    foreach (var s in properties.Skip(2).Select(a => $", {a.name}_table.{a.name}"))
                        sb.Append(s);
                    sb.Append(" FROM planets");
                    foreach (var prop in properties.Skip(2))
                    {
                        sb.Append($@"
                            FULL JOIN {prop.name}_table ON {prop.name}_table.id == planets.id
                        ");
                    }

                    command.CommandText = sb.ToString();
                    Items = new List<dynamic>();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dynamic obj = new ExpandoObject();
                            //obj.id = reader.GetInt32(0);
                            //obj.name = reader.GetString(1);
                            for (int i = 0; i < properties.Count; i++) { 
                                var prop = properties[i];
                                if (prop.type == typeof(string))
                                    ((IDictionary<string, Object>)obj)[prop.name] = reader.IsDBNull(i) ? "" : reader.GetString(i);
                                else if (prop.type == typeof(bool))
                                    if (reader.IsDBNull(i))
                                        ((IDictionary<string, Object>)obj)[prop.name] = false;
                                    else
                                    {
                                        var s = reader.GetString(i);
                                        ((IDictionary<string, Object>)obj)[prop.name] = s == "True";
                                        
                                    }

                                else if (prop.type == typeof(Int32))
                                    ((IDictionary<string, Object>)obj)[prop.name] = reader.IsDBNull(i) ? 0 : reader.GetInt32(i);
                                else
                                    throw new NotImplementedException();
                            }
                            Items.Add(obj);
                        }
                    }
                }
            }
        }
    }
}
