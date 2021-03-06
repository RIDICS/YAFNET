﻿<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="YafHeader.ascx.cs" Inherits="YAF.Controls.YafHeader" %>
<div id="yafheader">
    <!--<asp:Panel id="GuestUserMessage" CssClass="guestUser" runat="server" Visible="false">
       <asp:Label id="GuestMessage" runat="server"></asp:Label>
    </asp:Panel>-->
   
    <div class="outerMenuContainer">
        <asp:HyperLink runat="server" id="BannerLink" NavigateUrl="/" >
            <img src="~/forumlogo.png" runat="server" alt="logo" style="border: 0;" id="imgBanner" />
            <div class="forumlogoText">
                <h1><YAF:LocalizedLabel ID="ForumName" runat="server" LocalizedPage="TOOLBAR"
                                        LocalizedTag="FORUMNAME" /></h1>
            </div>
        </asp:HyperLink>
        
        <div class="menuContainer">
            <ul class="menuList">
                <asp:PlaceHolder ID="menuListItems" runat="server">
                </asp:PlaceHolder>
            </ul>
            <asp:Panel ID="quickSearch" runat="server" CssClass="QuickSearch" Visible="false">
               <asp:TextBox ID="searchInput" runat="server"></asp:TextBox>&nbsp;
               <asp:LinkButton ID="doQuickSearch" onkeydown="" runat="server" CssClass="QuickSearchButton"
                    OnClick="QuickSearchClick">
               </asp:LinkButton>
            </asp:Panel>
            <asp:PlaceHolder ID="AdminModHolder" runat="server" Visible="false">
              <ul class="menuAdminList">
                <asp:PlaceHolder ID="menuAdminItems" runat="server"></asp:PlaceHolder>
              </ul>
            </asp:PlaceHolder>
        </div>
        
        <asp:Panel id="UserContainer" CssClass="menuMyContainer" runat="server" Visible="false">
            <ul class="menuMyList">
                <asp:PlaceHolder ID="LoginItem" runat="server">
                </asp:PlaceHolder>
                <asp:PlaceHolder ID="MyProfile" runat="server">
                </asp:PlaceHolder>
                <asp:PlaceHolder ID="MyBuddiesItem" runat="server">
                </asp:PlaceHolder>
                <asp:PlaceHolder ID="MyAlbumsItem" runat="server">
                </asp:PlaceHolder>
                <asp:PlaceHolder ID="MyTopicItem" runat="server">
                </asp:PlaceHolder>
                <!-- logout btn -->
                <asp:PlaceHolder ID="LogutItem" runat="server" Visible="false">
                    <li class="menuAccount">
                        <asp:LinkButton ID="LogOutButton" runat="server" OnClick="LogOutClick" OnClientClick="createCookie('ScrollPosition',document.all ? document.scrollTop : window.pageYOffset);"></asp:LinkButton>
                    </li>
                </asp:PlaceHolder>
            </ul>
        </asp:Panel>
    </div>
    <div id="yafheaderEnd">
    </div>
</div>
