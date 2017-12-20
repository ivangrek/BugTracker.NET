<%@ Page language="C#"%>
<!--
Copyright 2002-2014 Corey Trager
Distributed under the terms of the GNU General Public License
-->
<!-- #include file = "inc.aspx" -->

<script language="C#" runat="server">

Security security;
    
///////////////////////////////////////////////////////////////////////
void Page_Load(Object sender, EventArgs e)
{

    btnet.Util.do_not_cache(Response);

    security = new Security();
    security.check_security(HttpContext.Current, Security.MUST_BE_ADMIN);
    
	int loop1, loop2;
	NameValueCollection coll;

	// Load ServerVariable collection into NameValueCollection object.
	coll=Request.ServerVariables;
	// Get names of all keys into a string array.
	String[] arr1 = coll.AllKeys;
	for (loop1 = 0; loop1 < arr1.Length; loop1++)
	{
	   Response.Write("Key: " + arr1[loop1] + "<br>");
	   String[] arr2=coll.GetValues(arr1[loop1]);
	   for (loop2 = 0; loop2 < arr2.Length; loop2++) {
		  Response.Write("Value " + loop2 + ": " + arr2[loop2] + "<br>");
	   }
	}

}



</script>


