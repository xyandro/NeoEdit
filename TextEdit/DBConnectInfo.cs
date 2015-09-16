using System;

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

		public string Test()
		{
			switch (Type)
			{
				case DBType.MySQL:
					try
					{
						var conn = new MySql.Data.MySqlClient.MySqlConnection(ConnectionString);
						conn.Open();
						return null;
					}
					catch (Exception ex) { return "Connection failed: " + ex.Message; }
				default: throw new ArgumentException("Invalid database type");
			}
		}
	}
}
