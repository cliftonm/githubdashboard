using System;
using System.Windows.Forms;

namespace GitHubDashboard
{
	public class OwnerDrawPanel : Panel
	{
		public OwnerDrawPanel()
		{
			SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
		}
	}
}
