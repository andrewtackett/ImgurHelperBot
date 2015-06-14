using System;
using RedditSharp;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;

namespace ImgurHelperBot
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Hello World!");
			HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("https://api.imgur.com/3/gallery/0ENTp");
			webRequest.Headers.Add("Authorization", "Client-ID b1949cd535a4805");
			Stream response = webRequest.GetResponse().GetResponseStream();
			StreamReader reader = new StreamReader(response);
			string responseFromServer = reader.ReadToEnd();

			Console.WriteLine(responseFromServer);
			reader.Close();
			response.Close();
		}
	}
}
