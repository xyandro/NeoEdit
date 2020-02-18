using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoEdit.Program.Transform;

namespace NeoEdit.Tests
{
	public partial class UnitTest
	{
		void VerifyDaterCreate(string str, string format, string defaultTimeZone, DateTimeOffset correctDTO, string correctStr = null)
		{
			var dto = Dater.StringToDateTimeOffset(str, format, defaultTimeZone);
			Assert.AreEqual(dto, correctDTO);
			var str2 = Dater.DateTimeOffsetToString(dto, format);
			Assert.AreEqual(str2, correctStr ?? str);
		}

		void VerifyDaterChangeTimeZone(string str, string newTimeZone, string correctStr)
		{
			var dto = Dater.StringToDateTimeOffset(str);
			dto = Dater.ChangeTimeZone(dto, newTimeZone);
			str = Dater.DateTimeOffsetToString(dto);
			Assert.AreEqual(str, correctStr);
		}

		[TestMethod]
		public void DaterTest()
		{
			VerifyDaterCreate("286296387.84", Dater.Unix, Dater.UTC, new DateTimeOffset(1979, 1, 27, 7, 46, 27, 840, TimeSpan.FromHours(-7)));
			VerifyDaterCreate("301934787.84", Dater.Unix, Dater.UTC, new DateTimeOffset(1979, 7, 27, 8, 46, 27, 840, TimeSpan.FromHours(-6)));

			VerifyDaterCreate("119307699878400000", Dater.FileTime, Dater.UTC, new DateTimeOffset(1979, 1, 27, 7, 46, 27, 840, TimeSpan.FromHours(-7)));
			VerifyDaterCreate("119464083878400000", Dater.FileTime, Dater.UTC, new DateTimeOffset(1979, 7, 27, 8, 46, 27, 840, TimeSpan.FromHours(-6)));

			VerifyDaterCreate("28882.6156", Dater.Excel, Dater.UTC, new DateTimeOffset(1979, 1, 27, 7, 46, 27, 840, TimeSpan.FromHours(-7)));
			VerifyDaterCreate("29063.6156", Dater.Excel, Dater.UTC, new DateTimeOffset(1979, 7, 27, 8, 46, 27, 840, TimeSpan.FromHours(-6)));

			VerifyDaterCreate("624218931878400000", Dater.Ticks, Dater.UTC, new DateTimeOffset(1979, 1, 27, 7, 46, 27, 840, TimeSpan.FromHours(-7)));
			VerifyDaterCreate("624375315878400000", Dater.Ticks, Dater.UTC, new DateTimeOffset(1979, 7, 27, 8, 46, 27, 840, TimeSpan.FromHours(-6)));

			VerifyDaterCreate("1979-01-27T07:46:27.8400000-07:00", Dater.RoundTripDateTime, Dater.UTC, new DateTimeOffset(1979, 1, 27, 7, 46, 27, 840, TimeSpan.FromHours(-7)));
			VerifyDaterCreate("1979-01-27T14:46:27.8400000", Dater.RoundTripDateTime, Dater.UTC, new DateTimeOffset(1979, 1, 27, 7, 46, 27, 840, TimeSpan.FromHours(-7)), "1979-01-27T14:46:27.8400000+00:00");
			VerifyDaterCreate("1979-01-27T07:46:27.8400000", Dater.RoundTripDateTime, "(UTC-07:00) Mountain Time (US & Canada)", new DateTimeOffset(1979, 1, 27, 7, 46, 27, 840, TimeSpan.FromHours(-7)), "1979-01-27T07:46:27.8400000-07:00");

			VerifyDaterCreate("1979-07-27T08:46:27.8400000-06:00", Dater.RoundTripDateTime, Dater.UTC, new DateTimeOffset(1979, 7, 27, 8, 46, 27, 840, TimeSpan.FromHours(-6)));
			VerifyDaterCreate("1979-07-27T14:46:27.8400000", Dater.RoundTripDateTime, Dater.UTC, new DateTimeOffset(1979, 7, 27, 8, 46, 27, 840, TimeSpan.FromHours(-6)), "1979-07-27T14:46:27.8400000+00:00");
			VerifyDaterCreate("1979-07-27T08:46:27.8400000", Dater.RoundTripDateTime, "(UTC-07:00) Mountain Time (US & Canada)", new DateTimeOffset(1979, 7, 27, 8, 46, 27, 840, TimeSpan.FromHours(-6)), "1979-07-27T08:46:27.8400000-06:00");

			VerifyDaterChangeTimeZone("1979-01-27T07:46:27.8400000-07:00", Dater.UTC, "1979-01-27T14:46:27.8400000+00:00");
			VerifyDaterChangeTimeZone("1979-07-27T08:46:27.8400000-06:00", Dater.UTC, "1979-07-27T14:46:27.8400000+00:00");
			VerifyDaterChangeTimeZone("1979-01-27T14:46:27.8400000+00:00", "(UTC-07:00) Mountain Time (US & Canada)", "1979-01-27T07:46:27.8400000-07:00");
			VerifyDaterChangeTimeZone("1979-01-27T08:46:27.8400000-06:00", "(UTC-07:00) Mountain Time (US & Canada)", "1979-01-27T07:46:27.8400000-07:00");
			VerifyDaterChangeTimeZone("1979-07-27T07:46:27.8400000-07:00", "(UTC-07:00) Mountain Time (US & Canada)", "1979-07-27T08:46:27.8400000-06:00");
			VerifyDaterChangeTimeZone("1979-01-27T14:46:27.8400000+00:00", "-07:00", "1979-01-27T07:46:27.8400000-07:00");
			VerifyDaterChangeTimeZone("1979-01-27T14:46:27.8400000+00:00", "-7", "1979-01-27T07:46:27.8400000-07:00");
			VerifyDaterChangeTimeZone("1979-01-27T14:46:27.8400000+00:00", "-7.5", "1979-01-27T07:16:27.8400000-07:30");
		}
	}
}
