using System;
using RedditSharp;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ImgurHelperBot
{
	class MainClass
	{
		public string getImgurImageLink(JObject json){
			string link = (string)json["data"]["link"];
			Console.WriteLine ("Link: " + link);
			return link;
		}

		public string[] getImgurGalleryLinks(JObject json){
			string[] test = {"test","test2"};

			return test;
		}

		public void getProperties(JObject json){
			foreach (JProperty property in json.Properties()){
				Console.WriteLine(property.Name + " - " + property.Value);
			}
		}

		public HttpWebRequest craftRequest(string url){
			string id = url.Substring(url.LastIndexOf("/") + 1);

			Console.WriteLine ("id: " + id + ", url: " + url);
			HttpWebRequest webRequest;

			if (url.Contains ("gallery")) {
				Console.WriteLine ("gallery");
				webRequest = (HttpWebRequest)WebRequest.Create("https://api.imgur.com/3/gallery/album/" + id);
			} else if(url.Contains("/a/")){
				Console.WriteLine ("album");
				webRequest = (HttpWebRequest)WebRequest.Create("https://api.imgur.com/3/album/" + id);
			} else {
				Console.WriteLine ("image");
				webRequest = (HttpWebRequest)WebRequest.Create("https://api.imgur.com/3/image/" + id);
			}

			return webRequest;
		}

		//Returns string containing response from server
		public string performRequest(HttpWebRequest webRequest){
			webRequest.Headers.Add("Authorization", "Client-ID b1949cd535a4805");
			Stream response = webRequest.GetResponse().GetResponseStream();
			StreamReader reader = new StreamReader(response);
			string responseFromServer = reader.ReadToEnd();

			reader.Close();
			response.Close();

			return responseFromServer;
		}

		public static void Main (string[] args)
		{
			MainClass myParser = new MainClass ();
			Console.WriteLine ("Hello World!");
			//string url = "http://imgur.com/cQH87QL";
			string url = "http://imgur.com/gallery/0ENTp";
			//string url = "http://imgur.com/a/G3oj0";
			HttpWebRequest webRequest;

			//Pick how to make the request based on the url, otherwise imgur will give us a 404 error
			webRequest = myParser.craftRequest(url);

			string responseFromServer = myParser.performRequest (webRequest);

			JObject json = JObject.Parse (responseFromServer);
			if (url.Contains ("gallery)")) {
				myParser.getProperties (json);
			} else {
				myParser.getImgurImageLink (json);
			}
			Console.WriteLine(responseFromServer);
		}
	}
}
