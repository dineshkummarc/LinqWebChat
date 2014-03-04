using System;
using System.Configuration;
using System.Linq;
using System.Text;

namespace LinqChat
{
    public partial class Chatroom : System.Web.UI.Page, System.Web.UI.ICallbackEventHandler
    {
        private string _callBackStatus;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // for simplity's sake we're going to assume that a
                // roomId was passed in the query string and that
                // it is an integer
                // note: in reality you would check if the roomId is empty
                // and is an integer
                string roomId = (string)Request["roomId"];
                lblRoomId.Text = roomId;

                this.GetRoomInformation();
                this.GetLoggedInUsers();
                this.InsertMessage(ConfigurationManager.AppSettings["ChatLoggedInText"] + " " + DateTime.Now.ToString());
                this.GetMessages();

                // create a call back reference so we can log-out user when user closes the browser
                string callBackReference = Page.ClientScript.GetCallbackEventReference(this, "arg", "LogOutUser", "");
                string logOutUserCallBackScript = "function LogOutUserCallBack(arg, context) { " + callBackReference + "; }";
                Page.ClientScript.RegisterClientScriptBlock(this.GetType(), "LogOutUserCallBack", logOutUserCallBackScript, true);
            }
        }

        private void GetRoomInformation()
        {
            // get the room information from the database
            // we're going to set this up so that we can use
            // many rooms if we want to
            LinqChatDataContext db = new LinqChatDataContext();

            var room = (from r in db.Rooms
                        where r.RoomID == Convert.ToInt32(lblRoomId.Text)
                        select r).SingleOrDefault();

            lblRoomId.Text = room.RoomID.ToString();
            lblRoomName.Text = room.Name;
        }

        protected void BtnSend_Click(object sender, EventArgs e)
        {
            if (txtMessage.Text.Length > 0)
            {
                this.InsertMessage(null);
                this.GetMessages();
                txtMessage.Text = String.Empty;
                ScriptManager1.SetFocus(txtMessage.ClientID);
            }
        }

        protected void Timer1_OnTick(object sender, EventArgs e)
        {
            this.GetLoggedInUsers();
            this.GetMessages();

            if ((string)Session["IsChatroomInFocus"] == null)
                ScriptManager1.SetFocus(txtMessage);
        }

        /// <summary>
        /// This will insert the passed text to the message table in the database
        /// </summary>
        private void InsertMessage(string text)
        {
            LinqChatDataContext db = new LinqChatDataContext();

            Message message = new Message();
            message.RoomID = Convert.ToInt32(lblRoomId.Text);
            message.UserID= Convert.ToInt32(Session["ChatUserID"]);

            if (String.IsNullOrEmpty(text))
            {
                message.Text = txtMessage.Text.Replace("<", "");
                message.Color = ddlColor.SelectedValue;
            }
            else
            {
                message.Text = text;
                message.Color = "gray";
            }

            message.ToUserID = null;            // in the future, we will use this value for private messages
            message.TimeStamp = DateTime.Now;
            db.Messages.InsertOnSubmit(message);
            db.SubmitChanges();
        }

        private void GetLoggedInUsers()
        {
            LinqChatDataContext db = new LinqChatDataContext();

            // let's check if this authenticated user exist in the
            // LoggedInUser table (means user is logged-in to this room)
            var user = (from u in db.LoggedInUsers
                        where u.UserID == Convert.ToInt32(Session["ChatUserID"])
                        && u.RoomID == Convert.ToInt32(lblRoomId.Text)
                        select u).SingleOrDefault();

            // if user does not exist in the LoggedInUser table
            // then let's add/insert the user to the table
            if (user == null)
            {
                LoggedInUser loggedInUser = new LoggedInUser();
                loggedInUser.UserID = Convert.ToInt32(Session["ChatUserID"]);
                loggedInUser.RoomID = Convert.ToInt32(lblRoomId.Text);
                db.LoggedInUsers.InsertOnSubmit(loggedInUser);
                db.SubmitChanges();
            }

            string userIcon;
            StringBuilder sb = new StringBuilder();

            // get all logged in users to this room
            var loggedInUsers = from l in db.LoggedInUsers
                                where l.RoomID == Convert.ToInt32(lblRoomId.Text)
                                select l;

            // list all logged in chat users in the user list
            foreach (var loggedInUser in loggedInUsers)
            {
                // show user icon based on sex
                if (loggedInUser.User.Sex.ToString().ToLower() == "m")
                    userIcon = "<img src='Images/manIcon.gif' style='vertical-align:middle' alt=''>  ";
                else
                    userIcon = "<img src='Images/womanIcon.gif' style='vertical-align:middle' alt=''>  ";

                if (loggedInUser.User.Username != (string)Session["ChatUsername"])
                        sb.Append(userIcon + "<a href=#>" + loggedInUser.User.Username + "</a><br>");
                else
                    sb.Append(userIcon + "<b>" + loggedInUser.User.Username + "</b><br>");
            }

            // holds the names of the users shown in the chatroom
            litUsers.Text = sb.ToString();
        }

        /// <summary>
        /// Get the last 20 messages for this room
        /// </summary>
        private void GetMessages()
        {
            LinqChatDataContext db = new LinqChatDataContext();

            var messages = (from m in db.Messages
                           where m.RoomID == Convert.ToInt32(lblRoomId.Text)
                           orderby m.TimeStamp descending
                           select m).Take(20).OrderBy(m => m.TimeStamp);

            if (messages != null)
            {
                StringBuilder sb = new StringBuilder();
                int ctr = 0;    // toggle counter for alternating color

                foreach (var message in messages)
                {
                    // alternate background color on messages
                    if (ctr == 0)
                    {
                        sb.Append("<div style='padding: 10px;'>");
                        ctr = 1;
                    }
                    else
                    {
                        sb.Append("<div style='background-color: #EFEFEF; padding: 10px;'>");
                        ctr = 0;
                    }

                    if (message.User.Sex.ToString().ToLower() == "m")
                        sb.Append("<img src='Images/manIcon.gif' style='vertical-align:middle' alt=''>  " + message.Text + "</div>");
                    else
                        sb.Append("<img src='Images/womanIcon.gif' style='vertical-align:middle' alt=''>  " + message.Text + "</div>");
                }

                litMessages.Text = sb.ToString();
            }
        }

        protected void BtnLogOut_Click(object sender, EventArgs e)
        {
            // log out the user by deleting from the LoggedInUser table
            LinqChatDataContext db = new LinqChatDataContext();

            var loggedInUser = (from l in db.LoggedInUsers
                               where l.UserID == Convert.ToInt32(Session["ChatUserID"])
                               && l.RoomID == Convert.ToInt32(lblRoomId.Text)
                               select l).SingleOrDefault();

            db.LoggedInUsers.DeleteOnSubmit(loggedInUser);
            db.SubmitChanges();

            // insert a message that this user has logged out
            this.InsertMessage("Just logged out! " + DateTime.Now.ToString());

            // clean the session
            Session.RemoveAll();
            Session.Abandon();
            
            // redirect the user to the login page
            Response.Redirect("Default.aspx");
        }
 
        #region ICallbackEventHandler Members

        string  System.Web.UI.ICallbackEventHandler.GetCallbackResult()
        {
            return _callBackStatus;
        }

        void  System.Web.UI.ICallbackEventHandler.RaiseCallbackEvent(string eventArgument)
        {
            _callBackStatus = "failed";

            // log out the user by deleting from the LoggedInUser table
            LinqChatDataContext db = new LinqChatDataContext();

            var loggedInUser = (from l in db.LoggedInUsers
                                where l.UserID == Convert.ToInt32(Session["ChatUserID"])
                                && l.RoomID == Convert.ToInt32(lblRoomId.Text)
                                select l).SingleOrDefault();

            db.LoggedInUsers.DeleteOnSubmit(loggedInUser);
            db.SubmitChanges();

            // insert a message that this user has logged out
            this.InsertMessage("Just logged out! " + DateTime.Now.ToString());

            _callBackStatus = "success";
        }

        #endregion
    }
}
