-- GodsWar Online: Seed data for PostgreSQL (accounts + godswar)

-- Legacy accounts from accounts.sql
INSERT INTO accounts.accounts_info
    (id, accounts, password, login_status, last_login_time, last_logout_time, last_login_ip, last_login_mac, total_online_time, accounts_status)
VALUES
    (125, 'updays', 'updays', 0, '2009-10-19 22:42:16', '2009-10-19 22:42:21', '127.0.0.1', 'updays', 44, 11),
    (126, '1110', '1110', 1, '2007-07-31 11:23:15', '2007-07-31 11:23:19', '192.168.1.101', '23565', 2545, 1),
    (127, '1111', '1111', 1, '2007-07-31 11:23:15', '2007-07-31 11:23:19', '192.168.1.101', '23565', 2545, 1),
    (128, '1112', '1112', 1, '2007-07-31 11:23:15', '2007-07-31 11:23:19', '192.168.1.101', '23565', 2545, 1),
    (129, '1113', '1113', 1, '2007-07-31 11:23:15', '2007-07-31 11:23:19', '192.168.1.101', '23565', 2545, 1),
    (130, '1114', '1114', 1, '2007-07-31 11:23:15', '2007-07-31 11:23:19', '192.168.1.101', '23565', 2545, 1),
    (131, '1115', '1115', 1, '2007-07-31 11:23:15', '2007-07-31 11:23:19', '192.168.1.101', '23565', 2545, 1),
    (132, 'test', 'test', 0, NOW(), NOW(), '127.0.0.1', '00:00:00:00:00:00', 0, 1),
    (133, 'test2', '123456', 0, NOW(), NOW(), '127.0.0.1', '00:00:00:00:00:00', 0, 1)
ON CONFLICT (accounts) DO NOTHING;

INSERT INTO accounts.accounts_register
    (acc_id, pass_name, pass_pwd, identity_id, real_name, email, question, answer, regedit_time, area_id, active_number, is_active, is_del)
VALUES
    (125, 'updays', 'updays', '1', 'updays', 'updays', 'updays', 'updays', '2009-10-19 22:43:16', '222', '12', NULL, NULL),
    (126, 'test', 'test', NULL, 'GodsWar Test', NULL, NULL, NULL, NOW(), '1', NULL, 1, 0),
    (127, 'test2', '123456', NULL, 'GodsWar Test 2', NULL, NULL, NULL, NOW(), '1', NULL, 1, 0)
ON CONFLICT (acc_id) DO NOTHING;

INSERT INTO accounts.gamearea_info
    (gid, game_area, acc_id, pass_name, pass_pwd, balance, is_del, is_draw)
VALUES
    (8226, 1, 125, 'updays', 'updays', NULL, 0, 0),
    (8227, 1, 0, '1110', '1110', NULL, 0, 0),
    (8228, 1, 0, '1111', '1111', NULL, 0, 0),
    (8229, 1, 0, '1112', '1112', NULL, 0, 0),
    (8230, 1, 0, '1113', '1113', NULL, 0, 0),
    (8231, 1, 0, '1114', '1114', NULL, 0, 0),
    (8232, 1, 0, '1115', '1115', NULL, 0, 0),
    (8233, 1, 132, 'test', 'test', NULL, 0, 0),
    (8234, 1, 133, 'test2', '123456', NULL, 0, 0)
ON CONFLICT (pass_name) DO NOTHING;

-- Legacy character from godswar.sql
INSERT INTO godswar.character_base
    (id, accounts, name, gender, gm, camp, profession, fighter_job_lv, scholar_job_lv, fighter_job_exp, scholar_job_exp,
     cur_hp, cur_mp, status, belief, prestige, earl_rank, consortia, consortia_job, consortia_contribute,
     store_num, bag_num, hair_style, face_shap, map, pos_x, pos_z, money, stone, skill_point, skill_exp,
     max_hp, max_mp, register_time, last_login_time, mutetime)
VALUES
    (3795, 'updays', '盘古', 'male', 0, 0, 1, 3, 3, 170, 170,
     1371, 391, 0, 0, 0, 0, 0, 0, 0,
     10, 1, 10, 0, 51, -16.46150, -102.64510, 2100, 0, 0, 24,
     1371, 391, '2009-10-20 18:10:01', '2009-10-20 19:15:02', 8)
ON CONFLICT (name) DO NOTHING;

INSERT INTO godswar.character_kitbag (user_id, kitbag_1, kitbag_2, kitbag_3, kitbag_4, storage, equip)
VALUES (
    3795,
    '[4000,,,,,,0,20,1,1,0]#[4030,,,,,,0,20,1,1,0]#[2900,,,,,,1,1,1,1,0]#[]#[]#[1000,0,,,,,1,1,1,1,0]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#',
    NULL, NULL, NULL,
    '[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[]#[0]#',
    '[]#[]#[2800,0,,,,,1,1,1,1,0]#[2100,,,,,,1,1,1,1,0]#[]#[]#[2900,50,,,,,1,1,1,1,0]#[]#[]#[]#[1400,,,,,,1,1,1,1,0]#[]#'
)
ON CONFLICT (user_id) DO NOTHING;

INSERT INTO godswar.user_altar (user_id) VALUES (3795) ON CONFLICT (user_id) DO NOTHING;
INSERT INTO godswar.user_magic (user_id, magic_info) VALUES (3795, '[250,0]#[3063,0]#') ON CONFLICT (user_id) DO NOTHING;
INSERT INTO godswar.user_quest (id, allqueststatus, activestatus, timequest)
VALUES (
    3795,
    '31,0,0,0,',
    '[5,0,4,0,0,0,0,0,7,0,0,0,0,]#',
    '[141,0,2,1256083260,0,0,0,0,0,0,0,0,]#[143,0,2,1256083260,0,0,0,0,0,0,0,0,]#[144,0,2,1256083260,0,0,0,0,0,0,0,0,]#'
)
ON CONFLICT (id) DO NOTHING;
INSERT INTO godswar.user_skill (user_id, skill_info)
VALUES (3795, '[50,1]#[51,0]#[52,0]#[53,0]#[54,0]#[55,0]#[56,0]#[57,0]#[58,0]#[59,0]#[60,0]#[61,0]#[62,0]#[63,0]#[64,0]#[65,0]#[66,0]#[67,0]#')
ON CONFLICT (user_id) DO NOTHING;

-- Advance identity sequences past seeded IDs
SELECT setval(pg_get_serial_sequence('accounts.accounts_info', 'id'), COALESCE((SELECT MAX(id) FROM accounts.accounts_info), 1));
SELECT setval(pg_get_serial_sequence('accounts.accounts_register', 'acc_id'), COALESCE((SELECT MAX(acc_id) FROM accounts.accounts_register), 1));
SELECT setval(pg_get_serial_sequence('accounts.gamearea_info', 'gid'), COALESCE((SELECT MAX(gid) FROM accounts.gamearea_info), 1));
SELECT setval(pg_get_serial_sequence('godswar.character_base', 'id'), COALESCE((SELECT MAX(id) FROM godswar.character_base), 1));
SELECT setval(pg_get_serial_sequence('godswar.consortia', 'id'), COALESCE((SELECT MAX(id) FROM godswar.consortia), 1));
