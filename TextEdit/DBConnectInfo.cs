using System;
using System.Data.Common;
using MySql.Data.MySqlClient;

namespace NeoEdit.TextEdit
{
	public class DBConnectInfo
	{
		public enum DBType
		{
			MySQL,
		}

		public string Name { get; set; }
		public DBType Type { get; set; }
		public string ConnectionString { get; set; }

		public DBConnectInfo Copy()
		{
			return new DBConnectInfo
			{
				Name = Name,
				ConnectionString = ConnectionString,
			};
		}

		public DbConnection GetConnection()
		{
			switch (Type)
			{
				case DBType.MySQL:
					{
						var conn = new MySqlConnection(ConnectionString);
						conn.Open();
						return conn;
					}
				default: throw new ArgumentException("Invalid database type");
			}
		}

		public string Test()
		{
			try
			{
				GetConnection();
				return null;
			}
			catch (Exception ex) { return "Connection failed: " + ex.Message; }
		}
	}
}
