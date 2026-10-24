-- The official MySQL image creates rasaauth and the 'rasa' account from
-- MYSQL_DATABASE, MYSQL_USER and MYSQL_PASSWORD before running this file.
-- This script is applied only on first initialization of an empty mysql_data volume.
CREATE DATABASE IF NOT EXISTS `rasachar` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE IF NOT EXISTS `rasaworld` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
GRANT ALL PRIVILEGES ON `rasachar`.* TO 'rasa'@'%';
GRANT ALL PRIVILEGES ON `rasaworld`.* TO 'rasa'@'%';
