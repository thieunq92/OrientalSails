﻿<%@ Page Title="" Language="C#" MasterPageFile="NewPopup.Master" AutoEventWireup="true" CodeBehind="FacilitieAdd.aspx.cs" Inherits="Portal.Modules.OrientalSails.Web.Admin.FacilitieAdd" %>

<asp:Content ID="Content2" ContentPlaceHolderID="AdminContent" runat="server">
    <script>
        function RefreshParentPage() {
            window.parent.closePoup(1);
        }
        function closePoup() {
            window.parent.closePoup(0);
        }
    </script>
    <div class="row">
        <div class="form-group">
            <div class="row">
                <div class="col-xs-2">
                    Tên
                </div>
                <div class="col-xs-10">
                    <asp:TextBox ID="txtName" runat="server" CssClass="form-control" placeholder="Tên"></asp:TextBox>
                </div>
            </div>
        </div>
<%--        <div class="form-group">--%>
<%--            <div class="row">--%>
<%--                <div class="col-xs-2">--%>
<%--                    Kho cha--%>
<%--                </div>--%>
<%--                <div class="col-xs-10">--%>
<%--                    <asp:DropDownList runat="server" ID="ddlParent" CssClass="form-control" />--%>
<%--                </div>--%>
<%--            </div>--%>
<%--        </div>--%>
        <div class="form-group">
            <div class="row">
                <div class="col-xs-12">
                    <asp:Button ID="btnSave" runat="server" OnClick="btnSave_Click" CssClass="btn btn-primary" /><asp:Button ID="btnCancel" runat="server" Text="Đóng" OnClientClick="closePoup();return false;" CssClass="btn btn-default" />
                </div>
            </div>
        </div>
    </div>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="Scripts" runat="server">
</asp:Content>
