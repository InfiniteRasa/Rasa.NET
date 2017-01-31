DROP TABLE IF EXISTS `equipableclass_slotid`;

DROP TABLE IF EXISTS `itemtemplate_weapon`;
CREATE TABLE IF NOT EXISTS `itemtemplate_weapon` (
  `itemTemplateId` int(11) NOT NULL,
  `clipSize` int(11) NOT NULL DEFAULT '0',
  `aimRate` double NOT NULL DEFAULT '0',
  `reloadTime` int(11) NOT NULL DEFAULT '0',
  `altActionId` int(11) NOT NULL DEFAULT '0',
  `altActionArg` int(11) NOT NULL DEFAULT '0',
  `aeType` int(11) NOT NULL DEFAULT '0',
  `aeRadius` int(11) NOT NULL DEFAULT '0',
  `recoilAmount` int(11) NOT NULL DEFAULT '0',
  `reuseOverride` int(11) NOT NULL DEFAULT '0',
  `coolRate` int(11) NOT NULL DEFAULT '0',
  `heatPerShot` double NOT NULL DEFAULT '0',
  `toolType` int(11) NOT NULL DEFAULT '0',
  `ammoPerShot` int(11) NOT NULL DEFAULT '0',
  `minDamage` int(11) NOT NULL DEFAULT '0',
  `maxDamage` int(11) NOT NULL DEFAULT '0',
  `ammoClassId` int(11) NOT NULL DEFAULT '0',
  `damageType` int(11) NOT NULL DEFAULT '0',
  `windupTime` int(11) NOT NULL DEFAULT '0',
  `recoveryTime` int(11) NOT NULL DEFAULT '0',
  `refireTime` int(11) NOT NULL DEFAULT '0',
  `range` int(11) NOT NULL DEFAULT '0',
  `altMaxDamage` int(11) NOT NULL DEFAULT '0',
  `altDamageType` int(11) NOT NULL DEFAULT '0',
  `altRange` int(11) NOT NULL DEFAULT '0',
  `altAERadius` int(11) NOT NULL DEFAULT '0',
  `altAEType` int(11) NOT NULL DEFAULT '0',
  `attackType` int(11) NOT NULL DEFAULT '0',
  PRIMARY KEY (`itemTemplateId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

INSERT INTO `itemtemplate_weapon` (`itemTemplateId`, `clipSize`, `aimRate`, `reloadTime`, `altActionId`, `altActionArg`, `aeType`, `aeRadius`, `recoilAmount`, `reuseOverride`, `coolRate`, `heatPerShot`, `toolType`, `ammoPerShot`, `minDamage`, `maxDamage`, `ammoClassId`, `damageType`, `windupTime`, `recoveryTime`, `refireTime`, `range`, `altMaxDamage`, `altDamageType`, `altRange`, `altAERadius`, `altAEType`, `attackType`) VALUES
	(17131, 20, 1, 1500, 1, 133, 0, 1, 1, 0, 1, 2, 15, 1, 59, 68, 3147, 1, 800, 1, 800, 80, 25, 1, 80, 1, 1, 2);
