﻿using System;
using System.Data.Common;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using NeoEdit.Common.Transform;

namespace NeoEdit.TextEdit
{
	public class DBConnectInfo
	{
		public enum DBType
		{
			MySQL,
			MSSQL,
		}

		public string Name { get; set; }
		public DBType Type { get; set; }
		string connectionString;
		const string encryptionKey = "<RSAKeyValue><Modulus>t5X3OepqSDm+YjqiW574OcoYoxLkt4tcEQB7B3+kR9rEVUSUocnQ2pA/rpz91u6crGORz5sUz8rp9NvWhrDeuN/KqZXo8cWGr2l7t6taiAxw+PwQ5f5UhboHOJcUwhqbivABX1yFNTbBZJAUDRNORmY2jhWZ94zVXagQ/FnRulU=</Modulus><Exponent>AQAB</Exponent><P>3qPtMt//q0h6SNUrrC+5DSqxCaCa7d8fQNXJwOdSp3dNX5o8j/MNFRIaJzCvFg6BUAwcpltjNERoKgfR/WoWew==</P><Q>0xf6qI1vTUue8HP4CGZGyItjdslkCIS6eS6MWvjsTdbwELLg13uEYBqM8ppdv0SAdsyZAu8XEx1SDehMwRqBbw==</Q><DP>CX3fnO2jzr+WRwifhgW60+7gAVMRh9adVHxIz6qNAYq6h7rhnhl0k1NkPgt7S2tu4+TAS+9VeWL5NeGDeFRPhQ==</DP><DQ>nw8EepkH8vA2NOzNSlb2owoUyl75l0mb0M/4Rlwmgoign5SJwxR5LIkVB4C1fve47MtByGorsuV2/K+7lg3I1Q==</DQ><InverseQ>qxfKZ8UAQDTevKy3D1b9LQdlqKPRGQPYteecFy3atM14wfWBAQICeSAZJzAdHjor4+r+UPN03ZqvbaLjKdJQkQ==</InverseQ><D>hWEBEyTKPtslBLzQxHwEoAfCSogpf2hSZU/SEqqbslCwn7qJudmkUYbHnZcVnRgS3/QfNZPYVPd5bpphi83ooXFm6z0PCwLXpGIz/Ogl9Ui+E836fpN4OZ5wehdFGZr6RLnpPppP7n1wEQ5lDzX43exjBB/8yGizESUf26E/Myk=</D></RSAKeyValue>";
		public string ConnectionString
		{
			get { return Coder.BytesToString(Crypto.Decrypt(Crypto.Type.RSAAES, Coder.StringToBytes(connectionString, Coder.CodePage.Base64), encryptionKey), Coder.CodePage.UTF8); }
			set { connectionString = Coder.BytesToString(Crypto.Encrypt(Crypto.Type.RSAAES, Coder.StringToBytes(value, Coder.CodePage.UTF8), encryptionKey), Coder.CodePage.Base64); }
		}

		public DBConnectInfo Copy()
		{
			return new DBConnectInfo
			{
				Name = Name,
				connectionString = connectionString,
			};
		}

		public DbConnection GetConnection()
		{
			DbConnection conn;
			switch (Type)
			{
				case DBType.MySQL: conn = new MySqlConnection(ConnectionString); break;
				case DBType.MSSQL: conn = new SqlConnection(ConnectionString); break;
				default: throw new ArgumentException("Invalid database type");
			}
			conn.Open();
			return conn;
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
