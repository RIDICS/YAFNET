[![YAF logo](http://yetanotherforum.net/forum/images/YafLogo.png)](http://www.yetanotherforum.net)

**YetAnotherForum.NET** (YAF.NET) ASP.NET Open Source Forum solution! The **YAF.NET** project is an international collaboration of like-minded, skilled, and creative individuals who are striving to make **YAF.NET** the most robust and malleable forum solutions available.

![projectbadge](http://www.ohloh.net/p/yaf/widgets/project_partner_badge.gif)

[![Build status](https://ci.appveyor.com/api/projects/status/9905j18xqb16gdy7?svg=true)](https://ci.appveyor.com/project/YAFNET/yafnet)

If you have any questions or would like to get in touch with the project, please see the contact information at the bottom of this document.

**Attention!** This fork is supposed to use only with the [Vokabulář Webový](https://github.com/RIDICS/ITJakub) and the OpenID Connect Authentication Provider.

## Installation Requirements

Make sure your server / Host has the following requirements:

**Minimum Version Supported**
* Windows 2008 Server (or above)
* IIS 7.0 (or above)
* ASP.NET 4.5.2 (or above)
* SQL Server 2008 (or above)

### STEP 0. Build

Run `BuildPackages.bat` to build solution and create ZIP packages in `deploy` folder.

### STEP 1. UNZIP

1.  The first step after downloading the Install Package, is to Unzip YAF.NET and copy the content to the physical location where the Application (YAF) will be run from. 

By default in IIS (Internet Information Server) expects the sites to be located at _c:\Inetpub\wwwroot\..._

### STEP 2. Configuring the Application in IIS (Internet Information Server)

1.  In IIS you need to create a new Virtual Directory, if you want to run YAF as application, that points to the physical directory where you extracted YAF in to.

2.  How to: [Create and Configure Virtual Directories in IIS](http://msdn.microsoft.com/en-us/library/bb763173.aspx)

3.  Make sure that the Application Pool for YAF is set to .NET 4

### STEP 3. SETUP DATABASE

1.  A valid database needs to exist on your SQL Server with proper permissions set so that YAF can access it. When you run YAF for the first time it will detect that the database is empty (or needs upgrading) and will automatically run you through the process
 required to create the SQL database structures needed.

### STEP 4. COPY WEB.CONFIG FILE

1.  You need to copy the file **recommended.web.config** to your yaf root Folder and rename it to
**web.config**. Warning: DO NOT edit the web.config unless you know what you're doing.

### STEP 4a. MODIFY &quot;web.config&quot; FILE:

Generate a Machine Key for your installation.

1.  Open the file web.config and visit our Support Site to...

[Generate a Machine Key](http://yetanotherforum.net/key). 

2.  Copy and paste the generated machine key to your web.config in the &lt;system.web&gt; section.

### STEP 4b. MODIFY &quot;app.config&quot; FILE:

1.  Set OIDC credentials (ClientID and ClientSecret) and authentication provider URL.

2.  Set key &quot;LoginCheckBasePath&quot; to JavaScript path on Auth service for check if user is logged in. Default value for Vokabulář Auth service is `/Account/CheckLogin`.

3.  Set YAF.ConfigPassword used e.g. for upgrade.

### STEP 4c. MODIFY &quot;mail.config&quot; FILE:

1.  Modify the SMTP settings by entering your mail server information. If you SMTP server requires SSL, you must add:

`<add key="YAF.UseSMTPSSL" value="true" />`

to your app.config or appSettings.

### STEP 4d. MODIFY &quot;db.config&quot; FILE:

1. Set connection string to the database.

### STEP 5. Run The Install Wizard

1.  Open the file install/default.aspx on your web site. If you are testing on your local computer, under a directory called YetAnotherForum.Net, the address should be: http://localhost/yetanotherforum.net/install/

2.  The wizard will guide you through the Install Process. In Install Process, you are going to create an admin user. This admin user must have the same username and email as an admin user in authentication provider. Without the same username and email, you will not be able to log in the forum and manage it.


## Community Support Forum

See a real live YAF Forum by visiting the [YAF Community Support forum](http://forum.yetanotherforum.net). Also, get your questions answered by the YAF community.

## License

Yet Another Forum.NET is licensed under the Apache 2.0 license. 


### Yet Another Forum Community Support

If you have any questions, please visit the [YAF Community Support forum](http://forum.yetanotherforum.net), or visit the [Wiki](https://github.com/YAFNET/YAFNET/wiki) for More Informations.

