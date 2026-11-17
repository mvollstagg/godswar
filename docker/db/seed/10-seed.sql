-- GodsWar seed data.
-- An account must exist in BOTH accounts_info (account list) and
-- gamearea_info (what the login stored procedure actually authenticates
-- against). accounts_register is the web-registration record.
-- Passwords are stored in plain text by this server, so seeding is trivial.
--
-- Default test account:  test / test
-- Extra accounts can be added with docker/tools/new-account.sql.

INSERT INTO accounts_info
    (accounts, password, login_status, last_login_time, last_logout_time,
     last_login_IP, last_login_mac, total_online_time, accounts_status)
VALUES
    ('test', 'test', 0, NOW(), NOW(), '127.0.0.1', '00:00:00:00:00:00', 0, 1)
ON DUPLICATE KEY UPDATE password = VALUES(password), accounts_status = 1;

INSERT INTO gamearea_info
    (GameArea, AccID, PassName, PassPwd, Balance, IsDel, IsDraw)
SELECT 1,
       (SELECT id FROM accounts_info WHERE accounts = 'test'),
       'test', 'test', NULL, 0, 0
WHERE NOT EXISTS (SELECT 1 FROM gamearea_info WHERE PassName = 'test');

INSERT INTO accounts_register
    (PassName, PassPwd, IdentityID, RealName, Email, Question, Answer,
     RegeditTime, AreaID, ActiveNumber, IsActive, IsDel)
SELECT 'test', 'test', NULL, 'GodsWar Test', NULL, NULL, NULL,
       NOW(), '1', NULL, 1, 0
WHERE NOT EXISTS (SELECT 1 FROM accounts_register WHERE PassName = 'test');
