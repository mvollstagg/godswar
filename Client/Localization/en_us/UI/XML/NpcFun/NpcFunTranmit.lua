local win = UIAPI:GetElement("FirstWin");
--传送
function NpcFunTranmit_SetUI(Type,Index)
	FirstWin_ButtonA1:Visible(true);
	FirstWin_ButtonA2:Visible(true);
	win:Visible(true);

end
--配置UI窗体信息生成自己想要的界面
function NpcFunTranmit_SetText(Type,Index,BtnID,SubID)
		if Index == 1 then
			if SubID== 1 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_1);
				Button:Visible(true);
			elseif SubID == 2 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_2);
				Button:Visible(true);
			elseif SubID == 3 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_3);
				Button:Visible(true);
			elseif SubID == 4 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_4);
				Button:Visible(true);
                	elseif SubID == 5 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_5);
				Button:Visible(true);
                	elseif SubID == 6 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_6);
				Button:Visible(true);
                	elseif SubID == 100 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_168);
				Button:Visible(true);
                	elseif SubID == 101 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_169);
				Button:Visible(true);
                	elseif SubID == 102 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_170);
				Button:Visible(true);
                	elseif SubID == 103 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_171);
				Button:Visible(true);
                	elseif SubID == 104 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_172);
				Button:Visible(true);
                	elseif SubID == 105 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_173);
				Button:Visible(true);
                	elseif SubID == 106 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_174);
				Button:Visible(true);
                	elseif SubID == 107 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_175);
				Button:Visible(true);
                	elseif SubID == 108 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_176);
				Button:Visible(true);
                	elseif SubID == 109 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_177);
				Button:Visible(true);
                	elseif SubID == 110 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_178);
				Button:Visible(true);
                	elseif SubID == 111 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_179);
				Button:Visible(true);
                	elseif SubID == 10 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_180);
				Button:Visible(true);
                	elseif SubID == 11 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_181);
				Button:Visible(true);
                	elseif SubID == 12 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_182);
				Button:Visible(true);
                	elseif SubID == 13 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_183);
				Button:Visible(true);
                	elseif SubID == 14 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_184);
				Button:Visible(true);
                	elseif SubID == 15 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_185);
				Button:Visible(true);
                	elseif SubID == 16 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_186);
				Button:Visible(true);
                	elseif SubID == 17 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_187);
				Button:Visible(true);
                	elseif SubID == 18 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_188);
				Button:Visible(true);
                	elseif SubID == 19 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_189);
				Button:Visible(true);
                	elseif SubID == 20 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_190);
				Button:Visible(true);
                	elseif SubID == 21 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_191);
				Button:Visible(true);
                	elseif SubID == 50 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_192);
				Button:Visible(true);
                	elseif SubID == 51 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_193);
				Button:Visible(true);
                	elseif SubID == 52 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_194);
				Button:Visible(true);
                	elseif SubID == 53 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_195);
				Button:Visible(true);
                	elseif SubID == 54 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_196);
				Button:Visible(true);
                	elseif SubID == 55 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_197);
				Button:Visible(true);
                	elseif SubID == 56 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_198);
				Button:Visible(true);
                	elseif SubID == 57 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_199);
				Button:Visible(true);
                	elseif SubID == 58 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_200);
				Button:Visible(true);
                	elseif SubID == 59 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_201);
				Button:Visible(true);
                	elseif SubID == 60 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_202);
				Button:Visible(true);
                	elseif SubID == 61 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_203);
				Button:Visible(true);
                	elseif SubID == 200 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_204);
				Button:Visible(true);
                	elseif SubID == 201 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_205);
				Button:Visible(true);
                	elseif SubID == 202 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_29);
				Button:Visible(true);
                	elseif SubID == 203 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_206);
				Button:Visible(true);
                	elseif SubID == 218 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_218);
				Button:Visible(true);
                	elseif SubID == 300 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_30);
				Button:Visible(true);
                	elseif SubID == 301 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_30);
				Button:Visible(true);
                	elseif SubID == 500 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_500);
				Button:Visible(true);
                	elseif SubID == 501 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_501);
				Button:Visible(true);
                	elseif SubID == 700 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_700);
				Button:Visible(true);
                	elseif SubID == 701 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_701);
				Button:Visible(true);
                	elseif SubID == 800 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_800);
				Button:Visible(true);
                	elseif SubID == 801 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_801);
				Button:Visible(true);
                	elseif SubID == 802 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_802);
				Button:Visible(true);
                	elseif SubID == 803 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_803);
				Button:Visible(true);
                	elseif SubID == 804 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_804);
				Button:Visible(true);
                	elseif SubID == 805 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_805);
				Button:Visible(true);
                	elseif SubID == 810 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_810);
				Button:Visible(true);
                	elseif SubID == 811 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_811);
				Button:Visible(true);
                	elseif SubID == 812 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_812);
				Button:Visible(true);
                	elseif SubID == 813 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_813);
				Button:Visible(true);
                	elseif SubID == 814 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_814);
				Button:Visible(true);
                	elseif SubID == 815 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_815);
				Button:Visible(true);
                	elseif SubID == 560 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_560);
				Button:Visible(true);
                	elseif SubID == 561 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_561);
				Button:Visible(true);
                	elseif SubID == 320 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_Y320);
				Button:Visible(true);
                	elseif SubID == 321 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_Y321);
				Button:Visible(true);
                	elseif SubID == 322 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_Y322);
				Button:Visible(true);
                	elseif SubID == 323 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_Y323);
				Button:Visible(true);
                	elseif SubID == 324 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_Y324);
				Button:Visible(true);
			elseif SubID == 8002 then
				FirstWin_Text1:SetText(NF_L0_Y8002);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 251 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_Y8003);
				Button:Visible(true);
			elseif SubID == 252 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_Y8003);
				Button:Visible(true);
			elseif SubID == 7002 then
				FirstWin_Text1:SetText(NF_L0_Y7002);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 253 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_Y253);
				Button:Visible(true);
			elseif SubID == 254 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_Y254);
				Button:Visible(true);
			elseif SubID == 10001 then
				FirstWin_Text1:SetText(NF_L0_10001);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 255 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_Y254);
				Button:Visible(true);
			elseif SubID == 256 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_Y254);
				Button:Visible(true);
			elseif SubID == 257 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_Y254);
				Button:Visible(true);
			elseif SubID == 258 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_Y253);
				Button:Visible(true);
			elseif SubID == 259 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_Y253);
				Button:Visible(true);
			elseif SubID == 260 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_Y253);
				Button:Visible(true);
			elseif SubID == 261 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_Y254);
				Button:Visible(true);
			elseif SubID == 262 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_Y254);
				Button:Visible(true);
			elseif SubID == 263 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_Y254);
				Button:Visible(true);
			elseif SubID == 264 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_Y253);
				Button:Visible(true);
			elseif SubID == 265 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_Y253);
				Button:Visible(true);
			elseif SubID == 266 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_Y253);
				Button:Visible(true);
			elseif SubID == 270 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_270);
				Button:Visible(true);
			elseif SubID == 271 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_271);
				Button:Visible(true);
			elseif SubID == 650 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_650);
				Button:Visible(true);
			elseif SubID == 651 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_651);
				Button:Visible(true);
			elseif SubID == 652 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_652);
				Button:Visible(true);
			elseif SubID == 653 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_653);
				Button:Visible(true);
			elseif SubID == 654 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_654);
				Button:Visible(true);
			elseif SubID == 655 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_655);
				Button:Visible(true);
			elseif SubID == 656 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_656);
				Button:Visible(true);
			elseif SubID == 657 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_657);
				Button:Visible(true);
			elseif SubID == 658 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_658);
				Button:Visible(true);
			elseif SubID == 659 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_659);
				Button:Visible(true);
			elseif SubID == 660 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_660);
				Button:Visible(true);
			elseif SubID == 661 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_661);
				Button:Visible(true);
			end;
		elseif Index == 2 then
			if SubID == 1000 then
				FirstWin_Text1:SetText(NF_L0_32);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 1001 then
				FirstWin_Text1:SetText(NF_L0_33);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 2001 then
				FirstWin_Text1:SetText(NF_L0_34);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 1009 then
				FirstWin_Text1:SetText(NF_L0_35);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 1010 then
				FirstWin_Text1:SetText(NF_L0_36);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 1200 then
				FirstWin_Text1:SetText(NF_L0_37);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 1111 then
				FirstWin_Text1:SetText(NF_L0_38);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 1112 then
				FirstWin_Text1:SetText(NF_L0_31);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 1113 then
				FirstWin_Text1:SetText(NF_L0_1113);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 1114 then
				FirstWin_Text1:SetText(NF_L0_1114);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 2700 then
				FirstWin_Text1:SetText(NF_L0_2700);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 2701 then
				FirstWin_Text1:SetText(NF_L0_2701);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 2702 then
				FirstWin_Text1:SetText(NF_L0_2702);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 2703 then
				FirstWin_Text1:SetText(NF_L0_2703);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 2704 then
				FirstWin_Text1:SetText(NF_L0_2704);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 2705 then
				FirstWin_Text1:SetText(NF_L0_2705);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 2706 then
				FirstWin_Text1:SetText(NF_L0_2706);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 512 then
				FirstWin_Text1:SetText(NF_L0_512);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 8000 then
				FirstWin_Text1:SetText(NF_L0_Y8000);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 8001 then
				FirstWin_Text1:SetText(NF_L0_Y8001);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 7001 then
				FirstWin_Text1:SetText(NF_L0_Y7001);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 1569 then
				FirstWin_Text1:SetText(NF_L0_1569);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 1019 then
				FirstWin_Text1:SetText(NF_L0_1019);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
                                NPCFUN:NeedMessage(False);
			elseif SubID == 3001 then
				FirstWin_Text1:SetText(NF_L0_3001);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 2727 then
				FirstWin_Text1:SetText(NF_L0_2727);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			end;
        	end;
end

function NpcFunTranmit_SetMsg(Type,Index,PreSubID,SubID)
		if SubID== 700 then
			FirstWin_Text1:SetText(NF_L0_TS700);
			FirstWin_Text1:Visible(true);
			NPCFUN:HaveMessageBox(true);
		elseif SubID== 701 then
			FirstWin_Text1:SetText(NF_L0_TS701);
			FirstWin_Text1:Visible(true);
			NPCFUN:HaveMessageBox(true);
		elseif SubID== 500 then
			FirstWin_Text1:SetText(NF_L0_TS500);
			FirstWin_Text1:Visible(true);
			NPCFUN:HaveMessageBox(true);
		elseif SubID == 202 then
			FirstWin_Text1:SetText(NF_L0_TS202);
			FirstWin_Text1:Visible(true);
			NPCFUN:HaveMessageBox(true);
		elseif SubID == 203 then
			FirstWin_Text1:SetText(NF_L0_TS203);
			FirstWin_Text1:Visible(true);
			NPCFUN:HaveMessageBox(true);
		elseif SubID == 501 then
			FirstWin_Text1:SetText(NF_L0_TS501);
			FirstWin_Text1:Visible(true);
			NPCFUN:HaveMessageBox(true);
                elseif SubID == 500000 then
			FirstWin_Text1:SetText(NF_L0_106);
			FirstWin_Text1:Visible(true);
			NPCFUN:HaveMessageBox(true);
                elseif SubID == 600000 then
			FirstWin_Text1:SetText(NF_L0_107);
			FirstWin_Text1:Visible(true);
			NPCFUN:HaveMessageBox(true);
		end;
end