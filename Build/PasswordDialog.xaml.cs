using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace Build
{
	partial class PasswordDialog
	{
		PasswordDialog()
		{
			InitializeComponent();
		}

		string decoded;
		void OkClick(object sender, RoutedEventArgs e)
		{
			try
			{
				var data = Convert.FromBase64String("mz+a0ikajQs02EsaYcpOoVEBQVLVR9A3vaTIiSi+SYqdGLtZwprzcxCiKmiH+lYDvSo2oopyoU1b2WzwNns2WA==");
				using (var alg = new AesCryptoServiceProvider())
				{
					alg.IV = Convert.FromBase64String("MKGBD+k3QXg2CaPp6BYPFg==");
					using (var byteGenerator = new Rfc2898DeriveBytes(password.Password, Convert.FromBase64String("AWdSJ9hs72TXUUqaKpYIbU2v/YONdOxf")))
						alg.Key = Convert.FromBase64String(Convert.ToBase64String(byteGenerator.GetBytes(32)));

					using (var decryptor = alg.CreateDecryptor())
						decoded = Encoding.UTF8.GetString(decryptor.TransformFinalBlock(data, 0, data.Length));

					if ((decoded.StartsWith("token:")) && (decoded.EndsWith(":token")))
					{
						decoded = decoded.Substring("token:".Length, decoded.Length - "token:".Length * 2);
						DialogResult = true;
						return;
					}
				}
			}
			catch { }
			MessageBox.Show("Invalid Password", "Error");
		}

		public static string Run()
		{
			var dialog = new PasswordDialog();
			if (dialog.ShowDialog() != true)
				return null;
			return dialog.decoded;
		}
	}
}
