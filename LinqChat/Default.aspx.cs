using System;
using System.Linq;
using System.Web.UI.WebControls;

namespace LinqChat
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void Login1_Authenticate(object sender, AuthenticateEventArgs e)
        {
            LinqChatDataContext db = new LinqChatDataContext();

            var user = (from u in db.Users
                        where u.Username == Login1.UserName
                        && u.Password == Login1.Password
                        select u).SingleOrDefault();

            if (user != null)
            {
                e.Authenticated = true;
                Session["ChatUserID"] = user.UserID;
                Session["ChatUsername"] = user.Username;
            }
            else
            {
                e.Authenticated = false;
            }
        }

        protected void Login1_LoggedIn(object sender, EventArgs e)
        {
            Response.Redirect("Chatroom.aspx?roomId=1");
        }
    }
}