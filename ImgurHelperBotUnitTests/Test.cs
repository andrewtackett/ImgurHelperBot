using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace ImgurHelperBot
{
	[TestFixture ()]
	public class Test
	{
		ImgurConnector myParse;

		[SetUp]
		public void ConnectorSetup(){
			myParse = new ImgurConnector ();
		}

		[Test ()]
		public void directLink()
		{
			testURL ("http://i.imgur.com/KkgrBY2.jpg",1);
		}

		[Test ()]
		//[Ignore ()]
		public void image ()
		{
			testURL("http://imgur.com/cQH87QL",1);
		}

		[Test ()]
		//[Ignore ()]
		public void album ()
		{
			testURL("http://imgur.com/a/G3oj0",23);
		}

		[Test ()]
		//[Ignore ()]
		public void galleryAlbum ()
		{
			testURL("http://imgur.com/gallery/UdaKS",18);
		}

		[Test ()]
		//[Ignore ()]
		public void galleryImage ()
		{
			testURL("http://imgur.com/gallery/A6DVKa3",1);
		}

		[Test ()]
		//[Ignore ()]
		public void galleryImageSuffixAdded ()
		{
			testURL("http://imgur.com/gallery/KmB7kFV/new?forcedesktop=1",1);
		}

		[Test ()]
		//[Ignore ()]
		public void albumSelectorAdded ()
		{
			testURL("https://imgur.com/a/QMmR2#0",22);
		}

		[Test ()]
		//[Ignore ()]
		public void imageListSelectorAdded ()
		{
			testURL("http://imgur.com/8UtzD83,6aG8utZ,RHymzpg,kmh2AUL,MOwVxNw,wbFYpJ1#5",6);
		}

		[Test ()]
		//[Ignore ()]
		public void galleryImageNonWorking ()
		{
			testURL("http://imgur.com/gallery/0ENTp",0);
		}

		[Test ()]
		public void galleryImageCommentSelectorAdded ()
		{
			testURL("http://imgur.com/gallery/3su3RVr/comment/444382081/1", 1);
		}

		public void testURL(string url,int numExpectedURLs)
		{
			List<string> urls = myParse.parseUrl(url);

			Assert.AreEqual (urls.Count, numExpectedURLs);
			Console.WriteLine ("num urls: " + urls.Count + ", expected: " + numExpectedURLs);
			//Assert.GreaterOrEqual (urls.Count,numExpectedURLs);
			for (int i = 0; i < urls.Count; i++) {
				string uriName = urls [i];
				Console.WriteLine ("testing url: " + urls [i]);
				Uri uriResult;
				Assert.True(Uri.TryCreate(uriName, UriKind.Absolute, out uriResult) && uriResult.Scheme == Uri.UriSchemeHttp);
			}
		}
	}
}

