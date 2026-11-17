-- Create an additional game account.
--
-- Usage (from the repo root, after `docker compose up -d`):
--   docker compose exec -T db mariadb -uroot -p"$DB_PASSWORD" accounts < docker/tools/new-account.sql
--
-- Edit the two values below before running.

SET @account = 'myaccount';
SET @password = 'mypassword';

INSERT INTO accounts_info
    (accounts, password, login_status, last_login_time, last_logout_time,
     last_login_IP, last_login_mac, total_online_time, accounts_status)
VALUES (@account, @password, 0, NOW(), NOW(), '127.0.0.1', '00:00:00:00:00:00', 0, 1)
ON DUPLICATE KEY UPDATE password = VALUES(password), accounts_status = 1;

-- The login procedure authenticates against gamearea_info, so the row is
-- mandatory there as well.
INSERT INTO gamearea_info (GameArea, AccID, PassName, PassPwd, Balance, IsDel, IsDraw)
SELECT 1, id, @account, @password, NULL, 0, 0 FROM accounts_info WHERE accounts = @account
ON DUPLICATE KEY UPDATE PassPwd = VALUES(PassPwd), IsDel = 0;

INSERT INTO accounts_register
    (PassName, PassPwd, IdentityID, RealName, Email, Question, Answer,
     RegeditTime, AreaID, ActiveNumber, IsActive, IsDel)
SELECT @account, @password, NULL, @account, NULL, NULL, NULL, NOW(), '1', NULL, 1, 0
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM accounts_register WHERE PassName = @account);

SELECT PassName, PassPwd FROM gamearea_info WHERE PassName = @account;
