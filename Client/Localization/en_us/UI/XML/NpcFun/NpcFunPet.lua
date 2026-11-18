local win = UIAPI:GetElement("FirstWin");
--宠物
function NpcFunPet_SetUI(Type,Index)
	FirstWin_ButtonA1:SetPosition(145,500);
	FirstWin_ButtonA2:SetPosition(220,500);
	FirstWin_ButtonA1:Visible(true);
	FirstWin_ButtonA2:Visible(true);
	win:Visible(true);

end

function NpcFunPet_SetText(Type,Index,BtnID,SubID)
	if Index == 1 then
	   if SubID == 1 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_LO_PET1);
	      Button:Visible(true);
	      Button:SetPosition(60,180);
	   elseif SubID == 2 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_LO_PET2);
	      Button:Visible(true);
	      Button:SetPosition(60,210);
	   elseif SubID == 3 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_LO_PET3);
	      Button:Visible(true);
	      Button:SetPosition(60,240);
	   elseif SubID == 4 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_LO_PET4);
	      Button:Visible(true);
	      Button:SetPosition(60,300);
	   elseif SubID == 5 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_LO_PET5);
	      Button:Visible(true);
	      Button:SetPosition(60,330);
	   elseif SubID == 6 then
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_LO_PET6);                      
	      Button:Visible(true);
	      Button:SetPosition(60,360);
          elseif SubID == 7 then
              print(BtnID,SubID);
              local Button = win:GetChild("FirstWin_Button" .. BtnID);
              Button:SetText(NF_LO_PET7);
              Button:Visible(true);
              Button:SetPosition(60,270);
          elseif SubID == 8 then
              local Button = win:GetChild("FirstWin_Button" .. BtnID);
              Button:SetText(NF_L0_P113);
              Button:Visible(true);
              Button:SetPosition(60,390);
	   end;
	elseif Index == 2 then
	   if SubID == 11 then
	      FirstWin_Text1:SetText(NF_LO_PET11);
	      FirstWin_Text1:Visible(true);
              FirstWin_Text1:SetPosition(40,100);
              FirstWin_Text2:SetText(NF_LO_PET1101);
              FirstWin_Text2:Visible(true);
              FirstWin_Text2:SetPosition(40,280);
	   elseif SubID == 101 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_LO_PET101);
	      Button:Visible(true);
              Button:SetPosition(60,405);
	   elseif SubID == 113 then
              FirstWin_ItemBtn1:Visible(true);
              FirstWin_ItemBtn1:SetPosition(60,400);
	      FirstWin_Text1:SetText(NF_L0_P114);
	      FirstWin_Text1:Visible(true);              
	   elseif SubID == 102 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_LO_PET102);
	      Button:Visible(true);
              Button:SetPosition(60,460);
	   elseif SubID == 12 then
	      FirstWin_Text1:SetText(NF_LO_PET12);
	      FirstWin_Text1:Visible(true);
              FirstWin_Text1:SetPosition(40,100);
	   elseif SubID == 13 then
	      FirstWin_Text1:SetText(NF_LO_PET13);
	      FirstWin_Text1:Visible(true);
              FirstWin_Text1:SetPosition(40,100);
              FirstWin_Text2:SetText(NF_LO_PET1301);
              FirstWin_Text2:Visible(true);
              FirstWin_Text2:SetPosition(40,350);
	   elseif SubID == 103 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_LO_PET103);
	      Button:Visible(true);
              Button:SetPosition(60,455);
	   elseif SubID == 14 then
	      FirstWin_Text1:SetText(NF_LO_PET14);
	      FirstWin_Text1:Visible(true);
	   elseif SubID == 104 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_LO_PET104);
	      Button:Visible(true);
	      Button:SetPosition(60,280);
	   elseif SubID == 15 then
              FirstWin_Text1:SetText(NF_LO_PET15);
              FirstWin_Text1:Visible(true);
              FirstWin_Text1:SetPosition(40,100);
              FirstWin_Text2:SetText(NF_LO_PET1501);
              FirstWin_Text2:Visible(true);
              FirstWin_Text2:SetPosition(40,260);
	   elseif SubID == 105 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_LO_PET105);
	      Button:Visible(true);
	      Button:SetPosition(60,360);
           elseif SubID == 16 then
              FirstWin_Text1:SetText(NF_LO_PET16);
              FirstWin_Text1:Visible(true);
              FirstWin_Text1:SetPosition(40,100);
	   elseif SubID == 106 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_LO_PET106);
	      Button:Visible(true);
	      Button:SetPosition(60,220);
	   elseif SubID == 107 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_LO_PET107);
	      Button:Visible(true);
	      Button:SetPosition(60,240);
	   elseif SubID == 108 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_LO_PET108);
	      Button:Visible(true);
	      Button:SetPosition(60,260);
	   elseif SubID == 109 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_LO_PET109);
	      Button:Visible(true);
	      Button:SetPosition(60,280);
	   elseif SubID == 110 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_LO_PET110);
	      Button:Visible(true);
	      Button:SetPosition(60,300);
	   elseif SubID == 111 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_LO_PET111);
	      Button:Visible(true);
	      Button:SetPosition(60,320);
	   elseif SubID == 1011 then
	      FirstWin_Text1:SetText(NF_LO_PET1011);
	      FirstWin_Text1:Visible(true);
	      NPCFUN:EndMessage(true);
	      NPCFUN:NeedMessage(False);
	   elseif SubID == 1061 then
	      FirstWin_Text1:SetText(NF_LO_PET1061);
	      FirstWin_Text1:Visible(true);
	      NPCFUN:EndMessage(true);
	      NPCFUN:NeedMessage(False);
	   elseif SubID == 1062 then
	      FirstWin_Text1:SetText(NF_LO_PET1062);
	      FirstWin_Text1:Visible(true);
	      NPCFUN:EndMessage(true);
	      NPCFUN:NeedMessage(False);
	   elseif SubID == 1075 then
	      FirstWin_Text1:SetText(NF_LO_PET1075);
	      FirstWin_Text1:Visible(true);
	      NPCFUN:EndMessage(true);
	      NPCFUN:NeedMessage(False);
           elseif SubID == 17 then
              FirstWin_Text1:SetText(NF_LO_PET17);
              FirstWin_Text1:Visible(true);
              FirstWin_Text1:SetPosition(40,100);
              FirstWin_Text2:SetText(NF_LO_PET1701);
              FirstWin_Text2:Visible(true);
              FirstWin_Text2:SetPosition(40,220);
           elseif SubID == 112 then
              print(BtnID,SubID);
              local Button = win:GetChild("FirstWin_Button" .. BtnID);
              Button:SetText(NF_LO_PET112);
              Button:Visible(true);
              Button:SetPosition(60,280);
	   end;
       elseif Index == 3 then
	   if SubID == 1011 then
	      FirstWin_Text1:SetText(NF_LO_PET1011);
	      FirstWin_Text1:Visible(true);
	      NPCFUN:EndMessage(true);
	      NPCFUN:NeedMessage(False);
	   elseif SubID == 137 then
	      FirstWin_Text1:SetText(NF_L0_P117);
	      FirstWin_Text1:Visible(true);
	      NPCFUN:EndMessage(true);
	      NPCFUN:NeedMessage(False);
	   elseif SubID == 138 then
	      FirstWin_Text1:SetText(NF_L0_P116);
	      FirstWin_Text1:Visible(true);
	      NPCFUN:EndMessage(true);
	      NPCFUN:NeedMessage(False);
	   elseif SubID == 139 then
	      FirstWin_Text1:SetText(NF_L0_P111);
	      FirstWin_Text1:Visible(true);
	      NPCFUN:EndMessage(true);
	      NPCFUN:NeedMessage(False);
	   elseif SubID == 130 then
	      FirstWin_Text1:SetText(NF_L0_P115);
	      FirstWin_Text1:Visible(true);
	      NPCFUN:EndMessage(true);
	      NPCFUN:NeedMessage(False);
	   elseif SubID == 1021 then
	      FirstWin_Text1:SetText(NF_LO_PET1021);
	      FirstWin_Text1:Visible(true);
	      NPCFUN:EndMessage(true);
	      NPCFUN:NeedMessage(False);
	   elseif SubID == 1022 then
	      FirstWin_Text1:SetText(NF_LO_PET1022);
	      FirstWin_Text1:Visible(true);
	      NPCFUN:EndMessage(true);
	      NPCFUN:NeedMessage(False);
	   elseif SubID == 1041 then
	      FirstWin_Text1:SetText(NF_LO_PET1041);
	      FirstWin_Text1:Visible(true);
	      NPCFUN:EndMessage(true);
	      NPCFUN:NeedMessage(False);
	   elseif SubID == 1051 then
	      FirstWin_Text1:SetText(NF_LO_PET1051);
	      FirstWin_Text1:Visible(true);
	      NPCFUN:EndMessage(true);
	      NPCFUN:NeedMessage(False);
	   elseif SubID == 1052 then
	      FirstWin_Text1:SetText(NF_LO_PET1052);
	      FirstWin_Text1:Visible(true);
	      NPCFUN:EndMessage(true);
	      NPCFUN:NeedMessage(False);
	   elseif SubID == 1053 then
	      FirstWin_Text1:SetText(NF_LO_PET1053);
	      FirstWin_Text1:Visible(true);
	      NPCFUN:EndMessage(true);
	      NPCFUN:NeedMessage(False);
	   elseif SubID == 1061 then
	      FirstWin_Text1:SetText(NF_LO_PET1061);
	      FirstWin_Text1:Visible(true);
	      NPCFUN:EndMessage(true);
	      NPCFUN:NeedMessage(False);
	   elseif SubID == 1062 then
	      FirstWin_Text1:SetText(NF_LO_PET1062);
	      FirstWin_Text1:Visible(true);
	      NPCFUN:EndMessage(true);
	      NPCFUN:NeedMessage(False);
	   elseif SubID == 1063 then
	      FirstWin_Text1:SetText(NF_LO_PET1063);
	      FirstWin_Text1:Visible(true);
	      NPCFUN:EndMessage(true);
	      NPCFUN:NeedMessage(False);
	   elseif math.mod(SubID ,1000) == 1 then
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_PET1064..((SubID-1)/1000));
	      Button:SetPosition(60,120);
	      Button:Visible(true)
	      NPCFUN:EndMessage(true);
	      Button:Enable(false);
	      UIAPI:SetChecked(false,Button);
	   elseif math.mod(SubID ,1000) == 2 then
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_PET1065..((SubID-2)/100000)-((SubID-2)/100000)%0.01);
	      Button:SetPosition(60,180);
	      Button:Visible(true)
	      NPCFUN:EndMessage(true);
	      Button:Enable(false);
	      UIAPI:SetChecked(false,Button);
	   elseif math.mod(SubID ,1000) == 3 then
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_PET1066..((SubID-3)/100000)-((SubID-3)/100000)%0.01);
	      Button:SetPosition(60,210);
	      Button:Visible(true)
	      NPCFUN:EndMessage(true);
	      Button:Enable(false);
	      UIAPI:SetChecked(false,Button);
	   elseif math.mod(SubID ,1000) == 4 then
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_PET1067..((SubID-4)/100000)-((SubID-4)/100000)%0.01);
	      Button:SetPosition(60,240);
	      Button:Visible(true)
	      NPCFUN:EndMessage(true);
	      Button:Enable(false);
	      UIAPI:SetChecked(false,Button);
	   elseif math.mod(SubID ,1000) == 5 then
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_PET1068..((SubID-5)/100000)-((SubID-5)/100000)%0.01);
	      Button:SetPosition(60,270);
	      Button:Visible(true)
	      NPCFUN:EndMessage(true);
	      Button:Enable(false);
	      UIAPI:SetChecked(false,Button);
	   elseif math.mod(SubID ,1000) == 6 then
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_PET1069..((SubID-6)/100000)-((SubID-6)/100000)%0.01);
	      Button:SetPosition(60,300);
	      Button:Visible(true)
	      NPCFUN:EndMessage(true);
	      Button:Enable(false);
	      UIAPI:SetChecked(false,Button);
	   elseif math.mod(SubID ,1000) == 7 then
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_PET1070..((SubID-7)/100000)-((SubID-7)/100000)%0.01);
	      Button:SetPosition(60,330);
	      Button:Visible(true)
	      NPCFUN:EndMessage(true);
	      Button:Enable(false);
	      UIAPI:SetChecked(false,Button);
	   elseif SubID == 1071 then
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_PET1071);
	      Button:SetPosition(60,360);
	      Button:Visible(true)
	      NPCFUN:EndMessage(true);
	      Button:Enable(false);
	      UIAPI:SetChecked(false,Button);
          elseif SubID == 1072 then
              FirstWin_Text1:SetText(NF_L0_PET1072);
              FirstWin_Text1:Visible(true);
              NPCFUN:EndMessage(true);
              NPCFUN:NeedMessage(False);
          elseif SubID == 1073 then
              FirstWin_Text1:SetText(NF_L0_PET1073);
              FirstWin_Text1:Visible(true);
              NPCFUN:EndMessage(true);
              NPCFUN:NeedMessage(False);
          elseif SubID == 1075 then
              FirstWin_Text1:SetText(NF_L0_PET1075);
              FirstWin_Text1:Visible(true);
              NPCFUN:EndMessage(true);
              NPCFUN:NeedMessage(False);
	   end;
	end;
end

function NpcFunPet_SetMsg(Type,Index,PreSubID,SubID)
	if Index == 1 then
	   if SubID== 6000 then
	   end; 
	end;    
	if Index == 2 then
	   if SubID== 101 then
	      PetAPI:pet_indenture(); ---灵魂契约UI                   
	   elseif SubID== 102 then
		  PetAPI:pet_samsara();-----宠物转生UI    
	   elseif SubID== 103 then
		  PetAPI:pet_inosculate();---宠物融合UI
	   elseif SubID== 106 then
		  PetAPI:pet_takePet_skillCount();
	   elseif SubID== 107 then
		  PetAPI:pet_takePet_skillCount();
	   end;
	end;
end

