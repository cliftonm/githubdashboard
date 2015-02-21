using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

using Clifton.Extensions;

using ForceDirectedGraph;

// To see how many accesses are remaining: curl -i https://api.github.com/events?access_token=0c01559caff94377b7fba76634fbca966590a675 > foo.txt

namespace GitHubDashboard
{
	public partial class Dashboard : Form
	{
		public static int CountThreshold = 0;

		protected string clientId;
		protected string secretId;
		protected string authCode;
		protected string accessToken;

		protected System.Windows.Forms.Timer timer;

		protected Diagram diagramEvents;
		protected Diagram diagramDescriptions;
		protected Diagram diagramLanguages;

		protected Node rootNodeEvents = null;
		protected Node rootNodeDescriptions = null;
		protected Node rootNodeLanguages = null;

		protected int totalEvents = 0;
		protected int totalDescriptionWords = 0;
		protected int totalLanguages = 0;

		protected Random rnd;
		protected Authorize auth;
		protected Thread queryThread;
		protected DateTime then;

		protected List<SpotNode> newNodes = new List<SpotNode>();
		protected int iteration = -1;
		protected Dictionary<string, string> eventIdTypeMap = new Dictionary<string, string>();
		protected Dictionary<string, TextNode> eventsWordNodeMap = new Dictionary<string, TextNode>();
		protected Dictionary<string, TextNode> descriptionsWordNodeMap = new Dictionary<string, TextNode>();
		protected Dictionary<string, TextNode> languagesWordNodeMap = new Dictionary<string, TextNode>();

		protected List<string> skipWords = new List<string>(new string[] { "a", "an", "and", "the", "it", "them", "their", "those", "us", "you", "I", "they", "in", "on", "with", "at", "under", "over", "above", "below",
			"we", "by", "to", "that", "can", "can't", "who", "are", "only", "now", "him", "her", "from", "he", "she", "for", "every", "so", "our", "of", "yours", "all", "was", "will", "is", "having", "as", "up", "down", "out", "after", "not", "be", "my", "rt",
			"this", "or", "nor", "these", "off", "on", "his", "its", "because", "no", "amp", "ur", "me", "how", "has", "have", "into"});

		public Dashboard()
		{
			InitializeComponent();
			GetAuthorization();
			Setup();
		}

		protected void GetAuthorization()
		{
			string[] tokens = File.ReadAllLines("authorization.txt");

			if (!String.IsNullOrEmpty(tokens[0]))
			{
				clientId = tokens[0];
			}

			if (!String.IsNullOrEmpty(tokens[1]))
			{
				secretId = tokens[1];
			}

			if ((tokens.Length > 2) && !String.IsNullOrEmpty(tokens[2]))
			{
				accessToken = tokens[2];
				StartQueryThread();
			}
			else
			{
				// We still need the access token, so open up a web browser window for the user to authorize the application.
				auth = new Authorize();
				auth.Show();
				auth.browser.Navigated += OnNavigated;
				auth.browser.Navigate("https://github.com/login/oauth/authorize?scope=user:notifications&client_id=" + clientId);
			}
		}

		/// <summary>
		/// Once the user authorizes the application, we get a "code" back from GitHub
		/// We use that code to obtain the access token.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void OnNavigated(object sender, WebBrowserNavigatedEventArgs e)
		{
			if (e.Url.Query.Contains("?code"))
			{
				authCode = e.Url.Query.RightOf("=");
				WebClient wc = new WebClient();
				accessToken = wc.DownloadString("https://github.com/login/oauth/access_token?client_id=" + clientId + "&client_secret=" + secretId + "&code=" + authCode + "&accept=json").Between("=", "&");
				auth.Close();
				
				File.WriteAllLines("authorization.txt", new string[] { clientId, secretId, accessToken });

				StartQueryThread();
			}
		}

		protected void StartQueryThread()
		{
			queryThread = new Thread(new ThreadStart(QueryGitHubThread));
			queryThread.IsBackground = true;
			queryThread.Start();
		}

		// We will have 3 panels:
		// 1. event type
		// 2. language
		// 3. word cloud of each project description

		protected void Setup()
		{
			rnd = new Random();

			diagramEvents = new Diagram();
			diagramEvents.Arrange();
			
			diagramDescriptions = new Diagram();
			diagramDescriptions.Arrange();
			
			diagramLanguages = new Diagram();
			diagramLanguages.Arrange();

			timer = new System.Windows.Forms.Timer();
			timer.Interval = 1000 / 20;		// 20 times a second, in milliseconds.
			timer.Tick += (sender, args) =>
				{
					pnlEvents.Invalidate(true);
					pnlDescriptions.Invalidate(true);
					pnlLanguages.Invalidate(true);
				};

			// Redraw the events word cloud.
			pnlEvents.Paint += (sender, args) =>
			{
				Graphics gr = args.Graphics;
				gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
				
				lock (this)
				{
					diagramEvents.Iterate(Diagram.DEFAULT_DAMPING, Diagram.DEFAULT_SPRING_LENGTH, Diagram.DEFAULT_MAX_ITERATIONS);
					diagramEvents.Draw(gr, Rectangle.FromLTRB((int)(pnlEvents.Width*.10), 10, (int)(pnlEvents.Width - pnlEvents.Width*.10), pnlEvents.Height - 20));
				}
			};

			// Redraw the descriptions word cloud.
			pnlDescriptions.Paint += (sender, args) =>
			{
				Graphics gr = args.Graphics;
				gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

				lock (this)
				{
					diagramDescriptions.Iterate(Diagram.DEFAULT_DAMPING, Diagram.DEFAULT_SPRING_LENGTH, Diagram.DEFAULT_MAX_ITERATIONS);
					diagramDescriptions.Draw(gr, Rectangle.FromLTRB((int)(pnlDescriptions.Width * .10), 10, (int)(pnlDescriptions.Width - pnlDescriptions.Width * .10), pnlDescriptions.Height - 20));
				}
			};

			// Redraw the languages word cloud.
			pnlLanguages.Paint += (sender, args) =>
			{
				Graphics gr = args.Graphics;
				gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

				lock (this)
				{
					diagramLanguages.Iterate(Diagram.DEFAULT_DAMPING, Diagram.DEFAULT_SPRING_LENGTH, Diagram.DEFAULT_MAX_ITERATIONS);
					diagramLanguages.Draw(gr, Rectangle.FromLTRB((int)(pnlLanguages.Width * .10), 10, (int)(pnlLanguages.Width - pnlLanguages.Width * .10), pnlLanguages.Height - 20));
				}
			};

			// Resize the panel when the form changes.
			SizeChanged += (sender, args) =>
				{
					int w3 = Width / 3 - 30;
					int h3 = Height - 30;

					pnlEvents.Size = new Size(w3, h3);
					pnlDescriptions.Location = new Point(10 + w3, 10);
					pnlDescriptions.Size = new Size(w3, h3);

					pnlLanguages.Location = new Point(20 + w3 * 2, 10);
					pnlLanguages.Size = new Size(w3, h3);
				};

			rootNodeEvents = new SpotNode(Color.Black);
			rootNodeDescriptions = new SpotNode(Color.Black);
			rootNodeLanguages = new SpotNode(Color.Black);

			diagramEvents.AddNode(rootNodeEvents);
			diagramDescriptions.AddNode(rootNodeDescriptions);
			diagramLanguages.AddNode(rootNodeLanguages);

			timer.Start();
		}

		/// <summary>
		/// Worker thread for querying github.
		/// </summary>
		protected void QueryGitHubThread()
		{
			then = DateTime.Now;

			while (true)
			{
				ElapseOneSecond();
				string data = GetData("https://api.github.com/events");

				if (!String.IsNullOrEmpty(data))
				{
					ProcessEvents(data);
				}
			}
		}

		/// <summary>
		/// To avoid exceeding the 5000 requests per hour limit, we ensure that we only 
		/// make one request a second (3600 requests per hour)
		/// </summary>
		protected void ElapseOneSecond()
		{
			int msToSleep = 1000 - (int)(DateTime.Now - then).TotalMilliseconds;
			then = DateTime.Now;

			// If there's any time remaining to sleep before our next query, do so now.
			if (msToSleep > 0)
			{
				Thread.Sleep(msToSleep);
			}
		}

		protected string GetData(string url)
		{
			string ret = String.Empty;
			HttpWebRequest request = WebRequest.Create(url + "?access_token=" + accessToken) as HttpWebRequest;
			request.Method = "GET";
			// After 3 hours of googling and reading answers on SO, I found that this is necessary.  Thank you Budda for posting that info.
			request.UserAgent = "Hello There";		

			// Other answers I found regarding the server error response, but that did not solve the problem:

			// This is unnecessary:
			// request.Accept = "application/json; charset=utf-8";
			// request.KeepAlive = false;
			// request.ContentType = "application/json; charset=utf-8";
			// request.UseDefaultCredentials = true;

			// Also this, in app.config, was not necessary:
			  //<system.net>
			  //  <settings>
			  //	<httpWebRequest useUnsafeHeaderParsing="true" />
			  //  </settings>
			  //</system.net>

			try
			{
				using (WebResponse response = request.GetResponse())
				{
					using (StreamReader reader = new StreamReader(response.GetResponseStream()))
					{
						ret = reader.ReadToEnd();
					}
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex);
			}

			return ret;
		}

		/// <summary>
		/// Process the event information.  Here, we extract the ID so we don't process the same event multiple times.
		/// We also get the repo URL and query the API for the repo's description and language, which are used to
		/// populate the other two word clouds.
		/// </summary>
		/// <param name="html"></param>
		protected void ProcessEvents(string html)
		{
			dynamic events = JsonConvert.DeserializeObject<List<Object>>(html);
			
			foreach (dynamic ev in events)
			{
				// TODO: Are we wanting to grow this infinitely?
				string id = ev.id.ToString();

				if (!eventIdTypeMap.ContainsKey(id))
				{
					string eventType = ev.type.ToString();
					eventIdTypeMap[id] = eventType;

					string repoUrl = ev.repo.url.ToString();
					ElapseOneSecond();		// Again, don't overtax the API.
					string repoData = GetData(repoUrl);

					if (!String.IsNullOrEmpty(repoData))
					{
						dynamic repoInfo = JsonConvert.DeserializeObject(repoData);
						string description = repoInfo.description;
						string language = repoInfo.language;

						// Don't collide with the WinForm thread's Paint functions.
						// TODO: Could be optimized a bit to spend less time in the locked state.
						lock (this)
						{
							if (!String.IsNullOrEmpty(eventType)) ++totalEvents;
							AddOrUpdateNode(eventType, rootNodeEvents, eventsWordNodeMap, () => totalEvents);

							if (!String.IsNullOrEmpty(language)) ++totalLanguages;
							AddOrUpdateNode(language, rootNodeLanguages, languagesWordNodeMap, () => totalLanguages);

							if (!String.IsNullOrEmpty(description))
							{
								description.Split(' ').ForEach(w =>
									{
										if (!EliminateWord(w))
										{
											// We never show more than 100 description words.
											if (descriptionsWordNodeMap.Count > 100)
											{
												RemoveAStaleWord(descriptionsWordNodeMap);
											}
											
											if (!String.IsNullOrEmpty(w)) ++totalDescriptionWords;
											AddOrUpdateNode(w, rootNodeDescriptions, descriptionsWordNodeMap, () => totalDescriptionWords);
										}
									});
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Only called for descriptions.  Events and languages can grow to whatever size we want.
		/// </summary>
		protected void RemoveAStaleWord(Dictionary<string, TextNode> wordNodeMap)
		{
			// TODO: Might be more efficient to maintain a sorted list to begin with!
			DateTime now = DateTime.Now;
			KeyValuePair<string, TextNode> tnode = wordNodeMap.OrderByDescending(w => (now - w.Value.UpdatedOn).TotalMilliseconds).First();
			// Do not call RemoveNode, as this results in a stack overflow because the property setter has this side effect.
			tnode.Value.Diagram = null;					// THIS REMOVES THE NODE FROM THE DIAGRAM.  
			wordNodeMap.Remove(tnode.Key);
			totalDescriptionWords -= tnode.Value.Count;
		}

		/// <summary>
		/// Update the specific diagram and word-node map.
		/// </summary>
		protected void AddOrUpdateNode(string word, Node rootNode, Dictionary<string, TextNode> wordNodeMap, Func<int> getTotalWords)
		{
			if (!String.IsNullOrEmpty(word))
			{
				if (!wordNodeMap.ContainsKey(word))
				{
					PointF p = rootNodeEvents.Location;
					TextNode n = new TextNode(word, p, getTotalWords);
					rootNode.AddChild(n);
					wordNodeMap[word] = n;
				}
				else
				{
					wordNodeMap[word].IncrementCount();
				}
			}
		}

		/// <summary>
		/// Remove common words.
		/// </summary>
		protected bool EliminateWord(string word)
		{
			bool ret = false;
			int n;

			if (int.TryParse(word, out n))
			{
				ret = true;
			}
			else
			{
				ret = skipWords.Contains(word.ToLower());
			}

			return ret;
		}
	}
}
