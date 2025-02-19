﻿<%@ Page Language="C#" Async="true" MasterPageFile="MO.Master" AutoEventWireup="true"
    CodeBehind="BookingReport.aspx.cs" Inherits="Portal.Modules.OrientalSails.Web.Admin.BookingReport" %>

<%@ MasterType VirtualPath="MO.Master" %>
<%@ Import Namespace="Portal.Modules.OrientalSails.Domain" %>
<%@ Import Namespace="Portal.Modules.OrientalSails.Web.Util" %>
<%@ Register Assembly="CMS.ServerControls" Namespace="CMS.ServerControls" TagPrefix="svc" %>
<asp:Content ID="Head" ContentPlaceHolderID="Head" runat="server">
    <title>Booking by date</title>
    <style>
        .star {
            color: #ff8d00;
            animation-name: blink;
            animation-duration: 2s;
            animation-iteration-count: infinite;
            display: inline-block;
            font-size: 20px
        }

        @keyframes blink {
            from {
                opacity: 0;
            }

            to {
                opacity: 1;
            }
        }
    </style>
</asp:Content>
<asp:Content ID="AdminContent" ContentPlaceHolderID="AdminContent" runat="server">
    <div id="bookingReportContainer" ng-controller="bookingReportController" ng-init="
         newExpense_OperatedName = '<%= CurrentUser.FullName %>';
         newExpense_OperatedId = <%= CurrentUser.Id%>;
         newExpense_OperatedPhone = '<%= CurrentUser.Phone %>';
         date = '<%= Date.ToString("dd/MM/yyyy") %>';
         cruiseId = <%= Cruise != null ? Cruise.Id : -1%>;
         tripId = <%= Trip != null ? Trip.Id : -1 %>;
         getListAllCruiseExpenseDTO();
         getListCruiseExpenseDTO();
         bookingGetAllByCriterion();
         shadowBookingGetByDate();
         LockingExpense = <%=LockingExpenseString %>;
         ">
        <div class="sticky" style="z-index: 999; background-color: #ffffff">
            <!--Phần chọn ngày xem booking-->
            <div class="form-group">
                <div class="row">
                    <div class="col-xs-2 nopadding-right">
                        <div class="input-group">
                            <asp:TextBox ID="txtDate" runat="server" CssClass="form-control" data-control="datetimepicker" autocomplete="off"></asp:TextBox>
                            <span class="input-group-btn">
                                <asp:Button ID="btnDisplay" runat="server" Text="Display" OnClick="btnDisplay_Click"
                                    CssClass="btn btn-primary" />
                            </span>
                        </div>
                    </div>
                </div>
            </div>
            <!---->
            <!--Các tab cruise-->
            <div class="form-group">
                <div class="row">
                    <div class="col-xs-12 btn-grid" id="cruiseTab">
                        <asp:Repeater ID="rptCruises" runat="server" OnItemDataBound="rptCruises_ItemDataBound">
                            <HeaderTemplate>
                                <asp:HyperLink ID="hplCruises" runat="server" Text="All" CssClass="btn btn-default" Target="_self" Style="padding: 3px 10px"></asp:HyperLink>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <asp:HyperLink ID="hplCruises" runat="server" CssClass="btn btn-default" data-cruiseid='<%# Eval("Id")%>' Target="_self" Style="width: auto; padding: 3px 3px"></asp:HyperLink>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>

                </div>
            </div>
            <div class="form-group">
                <div class="row">
                    <div class="col-xs-12 btn-grid">
                        <asp:Repeater ID="rptTrips" runat="server" OnItemDataBound="rptTrips_ItemDataBound">
                            <HeaderTemplate>
                                <asp:HyperLink ID="hplTrips" runat="server" Text="All" CssClass="btn btn-default" Target="_self" Style="padding: 3px 10px"></asp:HyperLink>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <asp:HyperLink ID="hplTrips" runat="server" CssClass="btn btn-default" data-cruiseid='<%# Eval("Id")%>' Target="_self" Style="width: auto; padding: 3px 3px"></asp:HyperLink>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </div>
            </div>
            <!---->
        </div>
        <div class="row">
            <div class="col-xs-12">
                <table class="table table-bordered table-common">
                    <tr class="header active">
                        <th rowspan="2"></th>
                        <th rowspan="2"></th>
                        <th rowspan="2">No
                        </th>
                        <th rowspan="2">Name of pax
                        </th>
                        <th colspan="3">Number of pax
                        </th>
                        <th rowspan="2">Trip
                        </th>
                        <th rowspan="2">Pickup address
                        </th>
                        <th rowspan="2">Special request
                        </th>
                        <th rowspan="2">Agency
                        </th>
                        <th rowspan="2">Booking code
                        </th>
                        <th rowspan="2">Sales In Charge
                        </th>
                        <th rowspan="2">Total
                        </th>
                        <th rowspan="2">Feedback</th>
                     <%--   <th rowspan="2">P/u time
                            <br />
                            <asp:Button runat="server" ID="btnSavePickupTime" Text="update" CssClass="btn btn-primary" OnClick="btnSavePickupTime_OnClick" />
                        </th>--%>
                    </tr>
                    <tr class="active">
                        <th>Adult
                        </th>
                        <th>Child
                        </th>
                        <th>Baby
                        </th>
                    </tr>
                    <tr ng-repeat="booking in listBooking" ng-class="{'custom-warning': booking.IsWarningBooking, 'custom-info': booking.IsPendingBooking}">
                        <td><i ng-show="booking.Inspection" class="fa fa-lg fa-clipboard-list text-disabled "
                            data-toggle="tooltip" title="Inspection"></i></td>
                        <td>
                            <i ng-show="booking.HaveBirthdayBooking" class="fa fa-lg fa-birthday-cake"
                                style="color: hotpink" data-toggle="tooltip" title="Birthday"></i>
                        </td>
                        <td>{{$index + 1}}</td>
                        <td class="--text-left"><span ng-bind-html="booking.CustomerName"></span></td>
                        <td>{{booking.Adult}}</td>
                        <td>{{booking.Child}}</td>
                        <td>{{booking.Baby}}</td>
                        <td>{{booking.TripCode}}</td>
                        <td class="--text-left"><span ng-bind-html="booking.PickupAddress"></span></td>
                        <td class="--text-left"><span ng-bind-html="booking.SpecialRequest"></span>
                            <br />
                            <span ng-bind-html="booking.SpecialRequestRoom"></span></td>
                        <td>
                            <% if (CanViewAgency)
                                { %>
                            <a ng-show="booking.AgencyNotes != ''" href="" data-toggle="tooltip" title="{{booking.AgencyNotes}}">
                                <i class="fa fa-lg fa-comment-dots icon icon__note"></i>
                            </a>
                            <br />
                            <a href="AgencyView.aspx?NodeId=1&SectionId=15&agencyid={{booking.AgencyId}}">{{booking.AgencyName}}</a>
                            <% } %>
                        </td>
                        <td>
                            <a href="BookingView.aspx?NodeId=1&SectionId=15&bi={{booking.Id}}">{{booking.BookingId}}</a>
                            <div ng-show="booking.HasInvoice" class="star" title="VAT">*</div>
                        </td>
                        <td>
                            <% if (CanViewAgency)
                                { %>
                            {{booking.SalesInChargeName}} <span ng-show="booking.SalesInChargePhone != ''">-</span> {{booking.SalesInChargePhone}}
                            <% } %>
                        </td>
                        <td><% if (CanViewTotal)
                                {%>
                            {{formatTotal(booking.Total)}} 
                            <% } %></td>
                        <td><a href="" ng-click="openPopupFeedback(booking.Id)">Feedback</a></td>
                    </tr>
                    <tr class="item">
                        <td colspan="4">
                            <strong>Grand total</strong>
                        </td>
                        <td>
                            <strong>{{totalAdult}}</strong>
                        </td>
                        <td>
                            <strong>{{totalChild}}</strong>
                        </td>
                        <td>
                            <strong>{{totalBaby}}</strong>
                        </td>
                        <td></td>
                        <td></td>
                        <td></td>
                        <td></td>
                        <td></td>
                        <td></td>
                        <td><% if (CanViewTotal)
                                { %><strong>{{totalOfTotalPrice}}</strong><% } %></td>
                        <td></td>
                    </tr>
                    <tr>
                        <td colspan="100%" class="custom-danger">Booking moved (to another date) or cancelled need attention (within 7 days if &lt;
                                    6 cabins and within 45 days if &gt;=6 cabins)
                            </td>
                    </tr>
                    <tr ng-repeat="booking in listBookingShadow" ng-class="{'custom-warning': booking.IsWarningBooking, 'custom-info': booking.IsPendingBooking}">
                        <td><i ng-show="booking.Inspection" class="fa fa-lg fa-clipboard-list text-disabled "
                            data-toggle="tooltip" title="Inspection"></i></td>
                        <td>
                            <i ng-show="booking.HaveBirthdayBooking" class="fa fa-lg fa-birthday-cake"
                                style="color: hotpink" data-toggle="tooltip" title="Birthday"></i>
                        </td>
                        <td>{{$index + 1}}</td>
                        <td class="--text-left"><span ng-bind-html="booking.CustomerName"></span></td>
                        <td>{{booking.Adult}}</td>
                        <td>{{booking.Child}}</td>
                        <td>{{booking.Baby}}</td>
                        <td>{{booking.TripCode}}</td>
                        <td class="--text-left"><span ng-bind-html="booking.PickupAddress"></span></td>
                        <td class="--text-left"><span ng-bind-html="booking.SpecialRequest"></span>
                            <br />
                            <span ng-bind-html="booking.SpecialRequestRoom"></span></td>
                        <td>
                            <% if (CanViewAgency)
                                { %>
                            <a ng-show="booking.AgencyNotes != ''" href="" data-toggle="tooltip" title="{{booking.AgencyNotes}}">
                                <i class="fa fa-lg fa-comment-dots icon icon__note"></i>
                            </a>
                            <br />
                            <a href="AgencyView.aspx?NodeId=1&SectionId=15&agencyid={{booking.AgencyId}}">{{booking.AgencyName}}</a>
                            <% } %>
                        </td>
                        <td>
                            <a href="BookingView.aspx?NodeId=1&SectionId=15&bi={{booking.Id}}">{{booking.BookingId}}</a>
                            <div ng-show="booking.HasInvoice" class="star" title="VAT">*</div>
                        </td>
                        <td>
                            <% if (CanViewAgency)
                                { %>
                                {{booking.SalesInChargeName}} <span ng-show="booking.SalesInChargePhone != ''">-</span> {{booking.SalesInChargePhone}}
                            <% } %>
                        </td>
                        <td><% if (CanViewTotal)
                                {%>
                                {{formatTotal(booking.Total)}} 
                            <% } %></td>
                        <td></td>
                    </tr>
                </table>
            </div>
        </div>

        <div ng-repeat="cruiseExpense in $root.listCruiseExpenseDTO" ng-show="isShowCruiseExpense('<%= CruiseIdAllow %>',cruiseExpense.Id.toString())">
            <div ng-show="cruiseExpense.ListGuideExpenseDTO.length == 0 && cruiseExpense.ListOthersExpenseDTO.length == 0">
                <div class="form-group">
                    <div class="row">
                        <div class="col-xs-2 nopadding-right" style="width: 11%">
                            <strong>{{cruiseExpense.Name}}</strong>
                        </div>
                        <div class="col-xs-offset-4 col-xs-1 nopadding-left" style="margin-left: 40.6%; padding-left: 33px !important;">
                            <div class="btn-group">
                                <a href="" class="dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false"
                                    style="box-shadow: none">
                                    <i class="fa fa-lg fa-plus-circle text-success" data-toggle="tooltip" data-placement="top" title="Add"
                                        ng-class="!LockingExpense ? 'text-success':'text-disabled'"></i>
                                    <span class="caret text-success" ng-class="!LockingExpense ? 'text-success':'text-disabled'"></span>
                                </a>
                                <ul class="dropdown-menu">
                                    <li><a href="" ng-click="!LockingExpense && addGuideExpenseDTO(cruiseExpense)">Guide</a></li>
                                    <li><a href="" ng-click="!LockingExpense && addOthersExpenseDTO(cruiseExpense)">Others</a></li>
                                </ul>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <div ng-show="cruiseExpense.ListGuideExpenseDTO.length != 0 || cruiseExpense.ListOthersExpenseDTO.length != 0">
                <div class="form-group" ng-repeat="guideExpense in cruiseExpense.ListGuideExpenseDTO">
                    <div class="row">
                        <input type="text" ng-show="false" ng-model="guideExpense.Id" />
                        <input type="text" ng-show="false" ng-model="guideExpense.GuideId" data-id="hidGuideId" />
                        <div class="col-xs-2 nopadding-right" style="width: 11%">
                            <strong><span ng-hide="$index != 0">{{cruiseExpense.Name}}</span></strong>
                        </div>
                        <div class="col-xs-2 nopadding-left nopadding-right">
                            <input type="text" placeholder="Guide name" readonly class="form-control" data-toggle="modal" data-target=".modal-selectGuide"
                                data-url="AgencySelectorPage.aspx?NodeId=1&SectionId=15" onclick="setTxtGuideClicked(this)"
                                data-id="txtName"
                                ng-model="guideExpense.GuideName"
                                ng-disabled="{{guideExpense.LockStatus==='Locked'}}">
                        </div>
                        <div class="col-xs-1 nopadding-left nopadding-right" style="width: 11%">
                            <input type="text" placeholder="Mobile" class="form-control" data-id="txtPhone" readonly ng-model="guideExpense.GuidePhone" data-control="phoneinputmask" />
                        </div>
                        <div class="col-xs-1 nopadding-left nopadding-right" style="width: 13%">
                            <div class="input-group">
                                <input type="text" placeholder="Cost" class="form-control" data-control="inputmask" value="0"
                                    ng-model="guideExpense.Cost" ng-readonly="{{guideExpense.LockStatus==='Locked'}}" />
                                <span class="input-group-addon">₫</span>
                            </div>
                        </div>
                        <div class="col-xs-1 nopadding-right" style="padding-top: 4px; width: 10%">
                            <a href="" ng-click="guideExpense.LockStatus!=='Locked' && removeGuideExpenseDTO(cruiseExpense,$index)"
                                type="button">
                                <i class="fa fa-lg fa-trash" ng-class="guideExpense.LockStatus==='Locked'? 'text-disabled':'text-danger'"
                                    data-toggle="tooltip" data-placement="top" title="Delete"></i></a>
                            <div class="btn-group" ng-class="{hide: $index >= 1}" style="vertical-align: top!important">
                                <a href="" class="dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false" style="box-shadow: none">
                                    <i class="fa fa-lg fa-plus-circle"
                                        ng-class="!LockingExpense ? 'text-success':'text-disabled'"
                                        data-toggle="tooltip" data-placement="top" title="Add"></i>
                                    <span class="caret text-success" ng-class="!LockingExpense ? 'text-success':'text-disabled'"></span>
                                </a>
                                <ul class="dropdown-menu">
                                    <li><a href="" ng-click="!LockingExpense && addGuideExpenseDTO(cruiseExpense)">Guide</a></li>
                                    <li><a href="" ng-click="!LockingExpense && addOthersExpenseDTO(cruiseExpense)">Others</a></li>
                                </ul>
                            </div>
                            <a href="" ng-click="cruiseExpense.getExpenseLockStatus()!=='Locked' && addGuideExpenseDTO(cruiseExpense)"
                                ng-class="{hide: $index == 0}">
                                <i class="fa fa-lg fa-plus-circle"
                                    ng-class="cruiseExpense.getExpenseLockStatus()!=='Locked'?'text-success':'text-disabled'"
                                    data-toggle="tooltip" data-placement="top" title="Add"></i></a>
                            <span class="caret text-success invisible" ng-class="{hide: $index == 0}"></span>
                            <a
                                href=""
                                ng-click="ExportTourByCruiseAndGuide(cruiseExpense,guideExpense)">
                                <i class="fa fa-lg fa-file-export text-warning" data-toggle="tooltip" data-placement="top" title="Export tour"></i>
                            </a>
                        </div>
                        <div class="col-xs-3">
                            <span>Operated by {{guideExpense.Operator_FullName}}, M: <span class="phone">{{guideExpense.Operator_Phone}}</span></span>
                            <input type="hidden" ng-model="guideExpense.operatedId">
                        </div>
                        <div class="col-xs-1" style="padding-top: 4px">
                            <a href="" data-toggle="modal" data-target=".modal-expenseHistory"
                                data-url="ExpenseHistory.aspx?NodeId=1&SectionId=15&ei={{guideExpense.Id}}"><i class="fa fa-lg fa-history"
                                    data-toggle="tooltip" data-placement="top" title="History"></i></a>
                        </div>
                    </div>
                </div>
                <div class="form-group" ng-repeat="othersExpense in cruiseExpense.ListOthersExpenseDTO">
                    <div class="row">
                        <input type="text" ng-show="false" ng-model="othersExpense.Id" />
                        <div class="col-xs-2 nopadding-right" style="width: 11%">
                            <strong><span ng-show="$index == 0 && cruiseExpense.ListGuideExpenseDTO.length == 0">{{cruiseExpense.Name}}</span></strong>
                        </div>
                        <div class="col-xs-2 nopadding-left nopadding-right">
                            <input type="text" class="form-control" placeholder="Others name" ng-model="othersExpense.Name"
                                ng-readonly="{{othersExpense.LockStatus==='Locked'}}" />
                        </div>
                        <div class="col-xs-1 nopadding-left nopadding-right" style="width: 11%">
                            <input type="text" placeholder="Mobile" class="form-control" ng-model="othersExpense.Phone"
                                data-control="phoneinputmask" ng-readonly="{{othersExpense.LockStatus==='Locked'}}" />
                        </div>
                        <div class="col-xs-1 nopadding-left nopadding-right" style="width: 13%">
                            <div class="input-group">
                                <input type="text" placeholder="Cost" class="form-control" data-control="inputmask" value="0" ng-model="othersExpense.Cost"
                                    ng-readonly="{{othersExpense.LockStatus==='Locked'}}" />
                                <span class="input-group-addon">₫</span>
                            </div>
                        </div>
                        <div class="col-xs-1 nopadding-right" style="padding-top: 4px; width: 10%">
                            <a href="" ng-click="othersExpense.LockStatus!=='Locked' && removeOthersExpenseDTO(cruiseExpense,$index)" type="button">
                                <i class="fa fa-lg fa-trash" ng-class="othersExpense.LockStatus!=='Locked'? 'text-danger':'text-disabled'"
                                    data-toggle="tooltip" data-placement="top" title="Delete"></i></a>
                            <div class="btn-group" ng-class="{hide: $index >= 1}" style="vertical-align: top!important">
                                <a class="dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false" style="box-shadow: none">
                                    <i class="fa fa-lg fa-plus-circle" ng-class="cruiseExpense.getExpenseLockStatus()!=='Locked'?'text-success':'text-disabled'"
                                        data-toggle="tooltip" data-placement="top" title="Add"></i>
                                    <span class="caret" ng-class="cruiseExpense.getExpenseLockStatus()!=='Locked'?'text-success':'text-disabled'"></span>
                                </a>
                                <ul class="dropdown-menu">
                                    <li><a href="" ng-click="cruiseExpense.getExpenseLockStatus()!=='Locked' && addGuideExpenseDTO(cruiseExpense)">Guide</a></li>
                                    <li><a href="" ng-click="cruiseExpense.getExpenseLockStatus()!=='Locked' && addOthersExpenseDTO(cruiseExpense)">Others</a></li>
                                </ul>
                            </div>
                            <a href="" ng-click="cruiseExpense.getExpenseLockStatus()!=='Locked' && addOthersExpenseDTO(cruiseExpense)"
                                ng-class="{hide: $index == 0}">
                                <i class="fa fa-lg fa-plus-circle"
                                    ng-class="cruiseExpense.getExpenseLockStatus()!=='Locked'?'text-success':'text-disabled'"
                                    data-toggle="tooltip" data-placement="top" title="Add"></i></a>
                            <span class="caret text-success invisible"></span>
                        </div>
                        <div class="col-xs-3">
                            <span>Operated by {{othersExpense.Operator_FullName}}, M: <span class="phone">{{othersExpense.Operator_Phone}}</span></span>
                            <input type="hidden" ng-model="othersExpense.operatedId">
                        </div>
                        <div class="col-xs-1" style="padding-top: 4px">
                            <a href="" data-toggle="modal" data-target=".modal-expenseHistory"
                                data-url="ExpenseHistory.aspx?NodeId=1&SectionId=15&ei={{othersExpense.Id}}"><i class="fa fa-lg fa-history"
                                    data-toggle="tooltip" data-placement="top" title="History"></i></a>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div>
            <div class="form-group">
                <div class="row">
                    <div class="col-xs-12">
                        <asp:UpdatePanel ID="UpdatePanel1" runat="server" style="display: inline">
                            <ContentTemplate>
                            </ContentTemplate>
                            <Triggers>
                                <asp:AsyncPostBackTrigger ControlID="btnSave" />
                            </Triggers>
                        </asp:UpdatePanel>
                        <asp:Button ID="btnSave" runat="server" OnClick="btnSave_Click" CssClass="btn btn-primary"></asp:Button>
                        <asp:Button ID="btnExport3Day" runat="server" OnClick="btnExport3Day_Click"
                            CssClass="btn btn-primary" Text="Export 3 days" />
                        <a class="btn btn-primary"
                            href="TransferRequestByDate.aspx?NodeId=1&SectionId=15&d=<%= Date.ToString("dd/MM/yyyy") %>"
                            ng-show="<%= Cruise == null || Cruise.Id <= 0 ? "true" : "false" %>">Transfer request</a>
                        <asp:Button ID="btnViewFeedback" runat="server" Text="View feedback" OnClick="btnViewFeedback_Click"
                            CssClass="btn btn-primary" />
                        <button
                            class="btn btn-primary" type="button"
                            ng-click="exportTourAll()">
                            Export all</button>
                        <asp:Button ID="btnExportCustomerData" runat="server" OnClick="btnExportCustomerData_Click"
                            CssClass="btn btn-primary" Text="Export customer"></asp:Button>
                        <asp:Button ID="btnProvisionalRegister" runat="server" OnClick="btnProvisionalRegister_Click"
                            CssClass="btn btn-primary" Text="Export provisional register"></asp:Button>
                        <button
                            class="btn btn-primary" type="button" id="btnLockDate"
                            data-uniqueid="<%= btnLockDate.UniqueID %>"
                            ng-click="lockDate()" ng-show="!LockingExpense">
                            Lock date
                       
                       
                        </button>
                        <asp:Button ID="btnLockDate" runat="server" OnClick="btnLockDate_Click" ng-show="false"></asp:Button>
                        <button class="btn btn-primary" type="button" ng-click="unlockDate()" data-uniqueid="<%= btnUnlockDate.UniqueID %>" id="btnUnlockDate"
                            ng-show="LockingExpense">
                            Unlock date</button>
                        <asp:Button ID="btnUnlockDate" runat="server" OnClick="btnUnlockDate_Click" ng-show="false"></asp:Button>
                        <asp:Button ID="btnExportXml" runat="server" OnClick="btnExportXml_OnClick"
                            CssClass="btn btn-primary" Text="Export XML"></asp:Button>
                    </div>
                </div>
            </div>
        </div>


        <div class="modal fade modal-selectGuide" tabindex="-1" role="dialog" aria-labelledby="gridSystemModalLabel">
            <div class="modal-dialog" role="document" style="width: 1230px">
                <div class="modal-content">
                    <div class="modal-header">
                        <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                        <h3 class="modal-title">Select guide</h3>
                    </div>
                    <div class="modal-body">
                        <iframe frameborder="0" width="1200" scrolling="no" onload="resizeIframe(this)" src=""></iframe>
                    </div>
                </div>
            </div>
        </div>

        <div class="modal fade modal-expenseHistory" tabindex="-1" role="dialog" aria-labelledby="gridSystemModalLabel">
            <div class="modal-dialog" role="document" style="width: 1230px">
                <div class="modal-content">
                    <div class="modal-header">
                        <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                        <h3 class="modal-title">Expense history</h3>
                    </div>
                    <div class="modal-body">
                        <iframe frameborder="0" width="1200" scrolling="no" onload="resizeIframe(this)" src=""></iframe>
                    </div>
                </div>
            </div>
        </div>
        <svc:Popup runat="server" ID="popupManager">
        </svc:Popup>
    </div>
</asp:Content>
<asp:Content ID="Scripts" ContentPlaceHolderID="Scripts" runat="server">
    <script type="text/javascript" src="/modules/sails/admin/bookingreportcontroller.js">
    </script>
    <script>
        $(function () {
            $(function () {
                $(".timepicker").datetimepicker({
                    datepicker: false,
                    timepicker: true,
                    format: 'H:i',
                    scrollInput: false,
                    scrollMonth: false
                });
            })
        });
        $('.modal-selectGuide').on('shown.bs.modal', function () {
            $(".modal-selectGuide iframe").attr('src', 'AgencySelectorPage.aspx?NodeId=1&SectionId=15&RoleId=20')
        })

        var txtGuideClicked = null;
        var rowGuideSelected = null;
        var txtGuideNameSelected = null;
        var txtPhoneSelected = null;
        var hidGuideIdSelected = null;
        function setTxtGuideClicked(txtGuide) {
            txtGuideClicked = txtGuide;
            if (typeof (txtGuideClicked) != "undefined") {
                rowGuideSelected = $(txtGuideClicked).closest(".row");
            }
        }

        var selectGuideIframe = $(".modal-selectGuide iframe");
        selectGuideIframe.on("load", function () {
            //giữ vị trí của scroll khi sang trang mới -- chức năng của phần selectguide
            if (window.name.search('^' + location.hostname + '_(\\d+)_(\\d+)_') == 0) {
                var name = window.name.split('_');
                $(".modal-selectGuide").scrollLeft(name[1]);
                $(".modal-selectGuide").scrollTop(name[2]);
                window.name = name.slice(3).join('_');
            }
            $(".pager a", selectGuideIframe.contents()).click(function () {
                window.name = location.hostname + "_" + $(".modal-selectGuide").scrollLeft() + "_" + $(".modal-selectGuide").scrollTop() + "_";
            })
            //--

            //chức năng select guide bằng popup
            $("[data-id = 'txtName']", selectGuideIframe.contents()).click(function () {
                if (typeof (txtGuideClicked) != "undefined") {
                    $(txtGuideClicked).val($(this).text())
                }
                if (typeof (rowGuideSelected) != "undefined") {
                    txtGuideNameSelected = $(rowGuideSelected).find("[data-id='txtName']");
                    txtPhoneSelected = $(rowGuideSelected).find("[data-id='txtPhone']");
                    hidGuideIdSelected = $(rowGuideSelected).find("[data-id='hidGuideId']");
                }
                if (typeof (txtPhoneSelected) != "undefined") {
                    $(txtPhoneSelected).val($(this).attr("data-phone"))
                }
                if (typeof (hidGuideIdSelected) != "undefined") {
                    $(hidGuideIdSelected).val($(this).attr("data-agencyid"));
                }
                if (typeof (txtGuideNameSelected) != "undefined") {
                    $(txtGuideNameSelected).val($(this).text())
                }
                $('.modal-selectGuide').modal('hide')
                $(hidGuideIdSelected).trigger('input');
                $(hidGuideIdSelected).trigger('change');
                $(txtGuideNameSelected).trigger('input');
                $(txtGuideNameSelected).trigger('change');
            });
            //--
        })
    </script>
    <script>
        $(document).ready(function () {
            new ResizeObserver(function () {
                setTimeout(function () {
                    $('.sticky').stick_in_parent({ parent: '.container-fluid', enable_bottoming: false })
                    //workaround refresh sticky when container resize
                    window.dispatchEvent(new Event('resize'));
                },1000);
            }).observe($('table')[0])
        });
    </script>
</asp:Content>
