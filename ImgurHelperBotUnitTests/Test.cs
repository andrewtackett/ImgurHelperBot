using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace ImgurHelperBot
{
	[TestFixture ()]
	public class Test
	{
		ImgurConnector myParse;
		//TODO: handle unusual urls: https://imgur.com/a/QMmR2#0
		/*entry point	content
			imgur.com/{image_id}	image
			imgur.com/{image_id}.extension	direct link to image (no html)
			imgur.com/a/{album_id}	album
			imgur.com/a/{album_id}#{image_id}	single image from an album
			imgur.com/gallery/{gallery_post_id}	gallery
			*/
		//http://imgur.com/8UtzD83,6aG8utZ,RHymzpg,kmh2AUL,MOwVxNw,wbFYpJ1#5
		//http://imgur.com/gallery/KmB7kFV/new?forcedesktop=1
		//string url = "http://imgur.com/cQH87QL";

		//nonworking - working
		//string url = "http://imgur.com/gallery/0ENTp";

		//gallery image - working
		//string url = "http://imgur.com/gallery/A6DVKa3";

		//gallery album - working
		//string url = "http://imgur.com/gallery/UdaKS";

		//album - working
		//string url = "http://imgur.com/a/G3oj0";

		[SetUp]
		public void ConnectorSetup(){
			myParse = new ImgurConnector ();
		}

		[Test ()]
		public void album ()
		{
			List<string> urls = myParse.parseUrl("http://imgur.com/a/G3oj0");

			Assert.GreaterOrEqual (urls.Count,0);
			for (int i = 0; i < urls.Count; i++) {
				string uriName = urls [i];
				Console.WriteLine ("testing url: " + urls [i]);
				Uri uriResult;
				Assert.True(Uri.TryCreate(uriName, UriKind.Absolute, out uriResult) && uriResult.Scheme == Uri.UriSchemeHttp);
			}
		}

		[Test ()]
		public void galleryAlbum ()
		{
			List<string> urls = myParse.parseUrl("http://imgur.com/gallery/UdaKS");

			Assert.GreaterOrEqual (urls.Count,0);
			for (int i = 0; i < urls.Count; i++) {
				string uriName = urls [i];
				Console.WriteLine ("testing url: " + urls [i]);
				Uri uriResult;
				Assert.True(Uri.TryCreate(uriName, UriKind.Absolute, out uriResult) && uriResult.Scheme == Uri.UriSchemeHttp);
			}
		}

		[Test ()]
		public void galleryImage ()
		{
			List<string> urls = myParse.parseUrl("http://imgur.com/gallery/A6DVKa3");

			Assert.GreaterOrEqual (urls.Count,0);
			for (int i = 0; i < urls.Count; i++) {
				string uriName = urls [i];
				Console.WriteLine ("testing url: " + urls [i]);
				Uri uriResult;
				Assert.True(Uri.TryCreate(uriName, UriKind.Absolute, out uriResult) && uriResult.Scheme == Uri.UriSchemeHttp);
			}
		}

		[Test ()]
		public void galleryImageNonWorking ()
		{
			List<string> urls = myParse.parseUrl("http://imgur.com/gallery/0ENTp");

			Assert.LessOrEqual (urls.Count,0);
		}
	}
}

