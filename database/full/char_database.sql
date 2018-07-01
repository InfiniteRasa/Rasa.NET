-- Depends on: nothing
CREATE TABLE `applied_patches` (
  `patch_name` VARCHAR(128) NOT NULL,
  `apply_time` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`patch_name`)
)
ENGINE=InnoDB
DEFAULT CHARACTER SET=utf8
COLLATE = utf8_general_ci;

-- Depends on: nothing
CREATE TABLE `account` (
  `id` INT(11) UNSIGNED NOT NULL,
  `email` VARCHAR(255) NOT NULL,
  `name` VARCHAR(64) NOT NULL,
  `level` TINYINT(3) UNSIGNED NOT NULL DEFAULT '0',
  `family_name` VARCHAR(64) NOT NULL DEFAULT '',
  `selected_slot` TINYINT(3) UNSIGNED NOT NULL DEFAULT '0',
  `can_skip_bootcamp` BIT(1) NOT NULL DEFAULT b'0',
  `last_ip` VARCHAR(15) NOT NULL DEFAULT '0.0.0.0',
  `last_login` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  
  PRIMARY KEY (`id`)
)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8
COLLATE = utf8_general_ci;

-- Depends on: account
CREATE TABLE `character` (
  `id` INT(11) UNSIGNED NOT NULL AUTO_INCREMENT,
  `account_id` INT(11) UNSIGNED NOT NULL,
  `slot` TINYINT(3) NOT NULL,
  `name` VARCHAR(64) NOT NULL,
  `class` INT(11) NOT NULL,
  `scale` DOUBLE NOT NULL,
  `gender` BIT(1) NOT NULL,
  `experience` INT(11) NOT NULL DEFAULT '0',
  `level` TINYINT(3) NOT NULL DEFAULT '1',
  `body` INT(11) NOT NULL,
  `mind` INT(11) NOT NULL,
  `spirit` INT(11) NOT NULL,
  `clone_credits` INT(11) NOT NULL DEFAULT '0',
  `race` INT(11) NOT NULL,
  `map_context_id` INT(11) NOT NULL,
  `coord_x` DOUBLE NOT NULL,
  `coord_y` DOUBLE NOT NULL,
  `coord_z` DOUBLE NOT NULL,
  `rotation` DOUBLE NOT NULL,
  `num_logins` INT(11) NOT NULL DEFAULT '0',
  `total_time_played` INT(11) NOT NULL DEFAULT '0',
  `last_played` datetime NULL,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  
  PRIMARY KEY (`id`),
  INDEX `character_index_account` (`account_id` ASC),
  CONSTRAINT `character_FK_account` FOREIGN KEY (`account_id`) REFERENCES `account`(`id`) ON DELETE RESTRICT ON UPDATE RESTRICT
)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8
COLLATE = utf8_general_ci;

-- Depends on: character
CREATE TABLE `character_appearance` (
  `character_id` INT(11) UNSIGNED NOT NULL,
  `slot` INT(11) UNSIGNED NOT NULL,
  `class` INT(11) UNSIGNED NOT NULL,
  `color` INT(11) UNSIGNED NOT NULL,
  
  PRIMARY KEY (`character_id`, `slot`),
  CONSTRAINT `character_appearance_FK_character` FOREIGN KEY (`character_id`) REFERENCES `rasachar`.`character` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT
)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8
COLLATE = utf8_general_ci;

-- Depends on: nothing
CREATE TABLE `clan` (
  `id` INT(11) UNSIGNED NOT NULL,
  `name` VARCHAR(100) NOT NULL,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  
  PRIMARY KEY (`id`)
)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8
COLLATE = utf8_general_ci;

-- Depends on: clan, character
CREATE TABLE `clan_member` (
  `clan_id` INT(11) UNSIGNED NOT NULL,
  `character_id` INT(11) UNSIGNED NOT NULL,
  `rank` TINYINT(3) UNSIGNED NOT NULL DEFAULT 0,
  
  PRIMARY KEY (`clan_id`, `character_id`),
  UNIQUE INDEX `clan_member_index_character` (`character_id` ASC),
  CONSTRAINT `clan_member_FK_clan` FOREIGN KEY (`clan_id`) REFERENCES `clan` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `clan_member_FK_character` FOREIGN KEY (`character_id`) REFERENCES `character` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT
)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8
COLLATE = utf8_general_ci;

