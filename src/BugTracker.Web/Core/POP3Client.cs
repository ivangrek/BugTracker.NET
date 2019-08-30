/*
    Copyright 2002 William J Dean
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

/*
    [Corey Trager] I downloaded this code from the URL below September 14, 2003:

    http://www.codeproject.com/csharp/pop3client.asp

    On that page Bill Dean writes:

    "I hope some of you find this useful.
    I'd love some feedback / comments.
    Please be aware that this code come with no warranty of any sort, express or implied.
    It is provided strictly "as is" and is indended solely for educational purposes.
    YOU USE IT AT YOUR OWN RISK.
    By using this code you agree to hold the Author and Restek blameless for any loss
    resulting from the use of the code."


    Here's a usage example from that page.
    static void Main(string[] args)
    {

        POP3Client.POP3client  Demo = new POP3Client.POP3client();
        Console.WriteLine ("****connecting to server:");
        Console.WriteLine (Demo.connect ("your_pop3_server"));
        Console.WriteLine ("****Issuing USER");
        Console.WriteLine (Demo.USER ("user_id"));
        Console.WriteLine ("****Issuing PASS");
        Console.WriteLine (Demo.PASS ("password"));
        Console.WriteLine ("****Issuing STAT");
        Console.WriteLine (Demo.STAT () );
        Console.WriteLine ("****Issuing LIST");
        Console.WriteLine (Demo.LIST () );
        Console.WriteLine ("****Issuing RETR 700...this will cause the POP3 server to gack a "
                                + "hairball since there is no message 700");
        Console.WriteLine (Demo.RETR (700) );    // this will cause the pop3 server to throw
                                                           // an error since there is no message 700
        Console.WriteLine ("****Issuing RETR 7");
        Console.WriteLine (Demo.RETR (7) );
        Console.WriteLine ("****Issuing QUIT");
        Console.WriteLine (Demo.QUIT ());

        Console.ReadLine ();
    }
*/

namespace BugTracker.Web.Core
{
    using System;
    using System.IO;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Text;

    //Please note that all code is copyright 2002 by William J Dean

    public class Pop3Client
    {
        public enum ConnectState
        {
            Disc,
            Authorization,
            Transaction,
            Update
        }

        private readonly string crlf = "\r\n";

        public bool BReadInputStreamCharByChar;
        private string data;
        public bool Error;
        private Stream netStrm;
        public string Pop;
        public int PopPort = 110;
        public bool PopSsl;
        public string Pwd;
        private StreamReader rdStrm;

        //borrowed from Agus Kurniawan's article:"Retrieve Mail From a POP3 Server Using C#"  at http://www.codeproject.com/csharp/popapp.asp
        private TcpClient server;
        public ConnectState State = ConnectState.Disc;
        private byte[] szData;

        public string UserName;

        public Pop3Client()
        {
            //nothing to do..just create to object
        }

        // This constructor added by Corey Trager Mar 2007
        public Pop3Client(bool bReadInputStreamCharByCharArg)
        {
            this.BReadInputStreamCharByChar = bReadInputStreamCharByCharArg;
        }

        public Pop3Client(string popServer, int popPort, bool popSsl, string userName, string password)
        {
            //put the specied server (pop_server), port (pop_port), if the server is an ssl server (pop_ssl),
            //user (user_name) and password (password) into the appropriate properties.
            this.Pop = popServer;
            this.PopPort = popPort;
            this.PopSsl = popSsl;
            this.UserName = userName;
            this.Pwd = password;
        }

        #region Utility Methods, some public, some private

        public string Connect(string popServer, int popPort, bool popSsl)
        {
            this.Pop = popServer; //put the specified server into the pop property
            this.PopPort = popPort; //put the specified port into the popPort property
            this.PopSsl = popSsl; //put the ssl into the popSSL property
            return Connect(); //call the connect method
        }

        public string Connect()
        {
            //Initialize to the pop server.  This code snipped "borrowed"
            //with some modifications...
            //from the article "Retrieve Mail From a POP3 Server Using C#" at
            //www.codeproject.com by Agus Kurniawan
            //http://www.codeproject.com/csharp/popapp.asp

            // create server with port 110

            try
            {
                this.server = new TcpClient(this.Pop, this.PopPort);
                // initialization
                if (!this.PopSsl)
                {
                    // initialization
                    this.netStrm = this.server.GetStream();
                }
                else
                {
                    this.netStrm = new SslStream(this.server.GetStream());
                    ((SslStream)this.netStrm).AuthenticateAsClient(this.Pop);
                }

                this.rdStrm = new StreamReader(this.netStrm);

                //The pop session is now in the AUTHORIZATION state
                this.State = ConnectState.Authorization;
                return this.rdStrm.ReadLine();
            }
            catch (Exception err)
            {
                return "Error: " + err;
            }
        }

        private string Disconnect()
        {
            var temp = "disconnected successfully.";
            if (this.State != ConnectState.Disc)
            {
                //close connection
                this.netStrm.Close();
                this.rdStrm.Close();
                this.State = ConnectState.Disc;
            }
            else
            {
                temp = "Not Connected.";
            }

            return temp;
        }

        private void IssueCommand(string command)
        {
            //send the command to the pop server.  This code snipped "borrowed"
            //with some modifications...
            //from the article "Retrieve Mail From a POP3 Server Using C#" at
            //www.codeproject.com by Agus Kurniawan
            //http://www.codeproject.com/csharp/popapp.asp
            this.data = command + this.crlf;
            this.szData = Encoding.ASCII.GetBytes(this.data.ToCharArray());
            this.netStrm.Write(this.szData, 0, this.szData.Length);
        }

        private string ReadSingleLineResponse()
        {
            //read the response of the pop server.  This code snipped "borrowed"
            //with some modifications...
            //from the article "Retrieve Mail From a POP3 Server Using C#" at
            //www.codeproject.com by Agus Kurniawan
            //http://www.codeproject.com/csharp/popapp.asp
            string temp;
            try
            {
                temp = this.rdStrm.ReadLine();
                was_pop_error(temp);
                return temp;
            }
            catch (Exception err)
            {
                return "Error in ReadSingleLineResponse(): " + err;
            }
        }

        // This was the original, but it didn't handle some UTF8 characters - Corey Trager, March 03, 2007
        private string read_multi_line_response()
        {
            //read the response of the pop server.  This code snipped "borrowed"
            //with some modifications...
            //from the article "Retrieve Mail From a POP3 Server Using C#" at
            //www.codeproject.com by Agus Kurniawan
            //http://www.codeproject.com/csharp/popapp.asp
            var temp = new StringBuilder(5000);
            string szTemp;

            try
            {
                szTemp = this.rdStrm.ReadLine();
                was_pop_error(szTemp);
                if (!this.Error)
                    while (szTemp != ".")
                    {
                        temp.Append(szTemp + this.crlf);
                        szTemp = this.rdStrm.ReadLine();
                    }
                else
                    return szTemp;

                return temp.ToString();
            }
            catch (Exception err)
            {
                return "Error in read_multi_line_response(): " + err;
            }
        }

        // written by Corey Trager, March 03, 2007
        private string NEW_read_multi_line_response()
        {
            var temp = new StringBuilder(4096);

            try
            {
                var b = new byte[4096];
                var bytesRead = 0;

                bytesRead = this.server.GetStream().Read(b, 0, b.Length);

                while (bytesRead > 0)
                {
                    for (var i = 0; i < bytesRead; i++) temp.Append(Convert.ToChar(b[i])); // Does work

                    if (temp.Length > 4
                        && temp[temp.Length - 1] == 0x0A
                        && temp[temp.Length - 2] == 0x0D
                        && temp[temp.Length - 3] == '.'
                        && temp[temp.Length - 4] == 0x0A
                        && temp[temp.Length - 5] == 0x0D)
                    {
                        temp[temp.Length - 3] = '\0';
                        bytesRead = 0;
                    }
                    else
                    {
                        bytesRead = this.server.GetStream().Read(b, 0, b.Length);
                    }
                }

                return temp.ToString();
            }
            catch (Exception err)
            {
                return "Error in read_multi_line_response(): " + err;
            }
        }

        private void was_pop_error(string response)
        {
            //detect if the pop server that issued the response believes that
            //an error has occured.

            if (response.StartsWith("-"))
                //if the first character of the response is "-" then the
                //pop server has encountered an error executing the last
                //command send by the client
                this.Error = true;
            else
                //success
                this.Error = false;
        }

        #endregion

        #region POP commands

        public string Dele(int msgNumber)
        {
            string temp;

            if (this.State != ConnectState.Transaction)
            {
                //DELE is only valid when the pop session is in the TRANSACTION STATE
                temp = "Connection state not = TRANSACTION";
            }
            else
            {
                IssueCommand("DELE " + msgNumber);
                temp = ReadSingleLineResponse();
            }

            return temp;
        }

        public string List()
        {
            var temp = "";
            if (this.State != ConnectState.Transaction)
            {
                //the pop command LIST is only valid in the TRANSACTION state
                temp = "Connection state not = TRANSACTION";
            }
            else
            {
                IssueCommand("LIST");
                temp = read_multi_line_response();
            }

            return temp;
        }

        public string List(int msgNumber)
        {
            var temp = "";

            if (this.State != ConnectState.Transaction)
            {
                //the pop command LIST is only valid in the TRANSACTION state
                temp = "Connection state not = TRANSACTION";
            }
            else
            {
                IssueCommand("LIST " + msgNumber);
                temp = ReadSingleLineResponse(); //when the message number is supplied, expect a single line response
            }

            return temp;
        }

        public string Noop()
        {
            string temp;
            if (this.State != ConnectState.Transaction)
            {
                //the pop command NOOP is only valid in the TRANSACTION state
                temp = "Connection state not = TRANSACTION";
            }
            else
            {
                IssueCommand("NOOP");
                temp = ReadSingleLineResponse();
            }

            return temp;
        }

        public string Pass()
        {
            string temp;
            if (this.State != ConnectState.Authorization)
            {
                //the pop command PASS is only valid in the AUTHORIZATION state
                temp = "Connection state not = AUTHORIZATION";
            }
            else
            {
                if (this.Pwd != null)
                {
                    IssueCommand("PASS " + this.Pwd);
                    temp = ReadSingleLineResponse();

                    if (!this.Error)
                        //transition to the Transaction state
                        this.State = ConnectState.Transaction;
                }
                else
                {
                    temp = "No Password set.";
                }
            }

            return temp;
        }

        public string Pass(string password)
        {
            this.Pwd = password; //put the supplied password into the appropriate property
            return Pass(); //call PASS() with no arguement
        }

        public string Quit()
        {
            //QUIT is valid in all pop states

            string temp;
            if (this.State != ConnectState.Disc)
            {
                IssueCommand("QUIT");
                temp = ReadSingleLineResponse();
                temp += this.crlf + Disconnect();
            }
            else
            {
                temp = "Not Connected.";
            }

            return temp;
        }

        public string Retr(int msg)
        {
            var temp = "";
            if (this.State != ConnectState.Transaction)
            {
                //the pop command RETR is only valid in the TRANSACTION state
                temp = "Connection state not = TRANSACTION";
            }
            else
            {
                // retrieve mail with number mail parameter
                IssueCommand("RETR " + msg);
                if (this.BReadInputStreamCharByChar)
                    temp = NEW_read_multi_line_response();
                else
                    temp = read_multi_line_response();
            }

            return temp;
        }

        public string Rset()
        {
            string temp;
            if (this.State != ConnectState.Transaction)
            {
                //the pop command STAT is only valid in the TRANSACTION state
                temp = "Connection state not = TRANSACTION";
            }
            else
            {
                IssueCommand("RSET");
                temp = ReadSingleLineResponse();
            }

            return temp;
        }

        public string Stat()
        {
            string temp;
            if (this.State == ConnectState.Transaction)
            {
                IssueCommand("STAT");
                temp = ReadSingleLineResponse();

                return temp;
            }

            //the pop command STAT is only valid in the TRANSACTION state
            return "Connection state not = TRANSACTION";
        }

        public string User()
        {
            string temp;
            if (this.State != ConnectState.Authorization)
            {
                //the pop command USER is only valid in the AUTHORIZATION state
                temp = "Connection state not = AUTHORIZATION";
            }
            else
            {
                if (this.UserName != null)
                {
                    IssueCommand("USER " + this.UserName);
                    temp = ReadSingleLineResponse();
                }
                else
                {
                    //no user has been specified
                    temp = "No User specified.";
                }
            }

            return temp;
        }

        public string User(string userName)
        {
            this.UserName = userName; //put the user name in the appropriate propertity
            return User(); //call USER with no arguements
        }

        #endregion
    }
}