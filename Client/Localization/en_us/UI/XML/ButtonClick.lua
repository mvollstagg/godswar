print("ButtonClick Init Success!!!");

local uiapi=UIAPI
local r,g,b,a=nil;

function UpListBoxBtn()

	local scroll = this:GetParent():GetChild("ListBox"):GetChild(0);
	local pos = uiapi:GetScrollPos(scroll);
	uiapi:SetScrollPos(pos-0.1,scroll);
end

function DownListBoxBtn()

	local scroll = this:GetParent():GetChild("ListBox"):GetChild(0);
	local pos = uiapi:GetScrollPos(scroll);
	uiapi:SetScrollPos(pos+0.1,scroll);
end


function UpContainerBtn()

	local scroll = this:GetParent():GetChild("Container"):GetChild(0);
	local pos = uiapi:GetScrollPos(scroll);
	uiapi:SetScrollPos(pos-0.1,scroll);
end

function DownContainerBtn()

	local scroll = this:GetParent():GetChild("Container"):GetChild(0);
	local pos = uiapi:GetScrollPos(scroll);
	uiapi:SetScrollPos(pos+0.1,scroll);
end


function UpContainer1Btn()

	local scroll = this:GetParent():GetChild("Container1"):GetChild(0);
	local pos = uiapi:GetScrollPos(scroll);
	uiapi:SetScrollPos(pos-0.1,scroll);
end

function DownContainer1Btn()

	local scroll = this:GetParent():GetChild("Container1"):GetChild(0);
	local pos = uiapi:GetScrollPos(scroll);
	uiapi:SetScrollPos(pos+0.1,scroll);
end

function UpContainer2Btn()

	local scroll = this:GetParent():GetChild("Container2"):GetChild(0);
	local pos = uiapi:GetScrollPos(scroll);
	uiapi:SetScrollPos(pos-0.1,scroll);
end

function DownContainer2Btn()

	local scroll = this:GetParent():GetChild("Container2"):GetChild(0);
	local pos = uiapi:GetScrollPos(scroll);
	uiapi:SetScrollPos(pos+0.1,scroll);
end

function EnterWebsiteBtn()
	uiapi:HyperLink("www.176.com");
end

function SetButtonAlpha()
	--this:SetFontAlpha(200);
	a,r,g,b=this:GetFontColor();
	this:SetFontColor(255 , 0 , 0 , 255);
end

function ReButtonAlpha()
	--this:SetFontAlpha(255);
	this:SetFontColor(r , g , b , a);
end

function AddYPosition()
	x , y = this:GetPosition();
	this:SetPosition(x , y-10);
end

function ReYPosition()
	x , y = this:GetPosition();
	this:SetPosition(x , y+10);
end


function AutoFightHelperFunc()

	str=SB_X1_15;
	uiapi:Helper(false,str,this:Instance());

end

function AutoConfigHelperFunc()

	str=SB_X1_14;
	uiapi:Helper(false,str,this:Instance());

end

function Skill1HelperFunc()

	str=SB_X1_17;
	uiapi:Helper(false,str,this:Instance());

end

--[[function Skill2HelperFunc()

	str=SB_X1_18;
	uiapi:Helper(false,str,this:Instance());

end--]]

--换装设定按钮悬停
function Switch1HelperFunc()

	str=IB_X0_8;
	uiapi:Helper(false,str,this:Instance());

end

--读取装备按钮悬停
function Switch2HelperFunc()

	str=IB_X0_9;
	uiapi:Helper(false,str,this:Instance());

end

function HelperLeftFunc()

	uiapi:Helper(false,'',this:Instance());

end

function GlOnClick()

	uiapi:HyperLink("http://gw.igg.com");

end

function GmmailOnClick()

	uiapi:HyperLink("http://service.igg.com/");

end

function RechargeClick()

	uiapi:HyperLink("http://pay.igg.com/");

end
function ConstellationBtndown_OnClick()
    win = uiapi:GetElement("ConstellationChooseWin");
	win:Visible(not win:IsVisible());
	if win:IsVisible() then
		win:Top();
	end

end



function MarketHelperFunc()

	str=IB_X0_10;
	uiapi:Helper(false,str,this:Instance());

end


--显示/隐藏地图设置窗口
function ViewSetBtn_OnClick()
    win = uiapi:GetElement("ViewSetWin");
	win:Visible(not win:IsVisible());
	if win:IsVisible() then
		win:Top();
		GameAPI:CompleteRookie(8)
	end

end

--显示/隐藏新手引导窗口
function RookieBtn_OnClick()
    win = uiapi:GetElement("RookieGuideWin");
	win:Visible(not win:IsVisible());
	if win:IsVisible() then
		win:Top();
	end
	RookieBtn_SetFlash(false)

end


function AutoSeekWayBtn_OnClick()
    win = uiapi:GetElement("AudioSetupListUI");
	win:Visible(not win:IsVisible());
	if win:IsVisible() then
		win:Top();
	end

end

--聊天帮助按钮点击事件
function CharHelpBtn_OnClick()
    win = uiapi:GetElement("CharHelpWin");
	win:Visible(not win:IsVisible());
	if win:IsVisible() then
		win:Top();
	end

end

function ToptenBtn_OnClick()
	serverid = PlayerAPI:GetServerID()
	--camp = LUA_GameData:GetFaction()
	--str ="http://zs.176.com/rank/?key="..serverid.."&c="..camp

	--uiapi:HyperLink(str);

	--test
    win = uiapi:GetElement("RookieGuideWin");
	win:Visible(not win:IsVisible());
	if win:IsVisible() then
		win:Top();
	end
end

--右边技能栏缩放按钮点击事件
function HideBtn_OnClick()
   
    win = uiapi:GetElement("SkillUI");
	
	Skill17Bk = win:GetChild("Skill17Bk");
	Skill17Bk:Visible(not Skill17Bk:IsVisible());
	
	Skill18Bk = win:GetChild("Skill18Bk");
	Skill18Bk:Visible(not Skill18Bk:IsVisible());
	
	Skill19Bk = win:GetChild("Skill19Bk");
	Skill19Bk:Visible(not Skill19Bk:IsVisible());
	
	Skill20Bk = win:GetChild("Skill20Bk");
	Skill20Bk:Visible(not Skill20Bk:IsVisible());
	
	Skill21Bk = win:GetChild("Skill21Bk");
	Skill21Bk:Visible(not Skill21Bk:IsVisible());
	
	
	
	Skill17 = win:GetChild("Skill17");
	Skill17:Visible(not Skill17:IsVisible());
	
	Skill18 = win:GetChild("Skill18");
	Skill18:Visible(not Skill18:IsVisible());
	
	Skill19 = win:GetChild("Skill19");
	Skill19:Visible(not Skill19:IsVisible());
	
	Skill20 = win:GetChild("Skill20");
	Skill20:Visible(not Skill20:IsVisible());
	
	Skill21 = win:GetChild("Skill21");
	Skill21:Visible(not Skill21:IsVisible());
	
	
	
	KeyCtrl1 = win:GetChild("KeyCtrl1");
	KeyCtrl1:Visible(not KeyCtrl1:IsVisible());
	
	KeyCtrl2 = win:GetChild("KeyCtrl2");
	KeyCtrl2:Visible(not KeyCtrl2:IsVisible());
	
	KeyCtrl3 = win:GetChild("KeyCtrl3");
	KeyCtrl3:Visible(not KeyCtrl3:IsVisible());
	
	KeyCtrl4 = win:GetChild("KeyCtrl4");
	KeyCtrl4:Visible(not KeyCtrl4:IsVisible());
	
	KeyCtrl5 = win:GetChild("KeyCtrl5");
	KeyCtrl5:Visible(not KeyCtrl5:IsVisible());
	
	
end

--排行榜按钮点击事件
function ToptenBtn_OnClick()
	--local serverId = PlayerAPI:GetServerID()
	--local camp = PlayerAPI:GetGameData():GetFaction()
	--local str = "http://gw.igg.com/grade.php"..serverId.."&c="..camp
	local str  = "http://gw.igg.com/grade.php"
	uiapi:HyperLink(str);
	
end

--服务器时间悬停
function SeverTime_OnHov()

	str=BC_X0_1;
	uiapi:Helper(false,str,this:Instance());

end

