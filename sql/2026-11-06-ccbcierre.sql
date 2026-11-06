-- Meses cerrados por cuenta bancaria (SD1401, Cerrar Mes). La tabla no existía en ningún catálogo.
-- mes: MMYYYY, igual que Format(fecha, "MMYYYY") en ModBancos.bas.
-- Vacía no cambia nada. Al cerrar un mes, anular un movimiento de ese mes pasa a generar una
-- reversión en lugar de borrarlo, también en VB6.
CREATE TABLE IF NOT EXISTS `ccbcierre` (
  `banco` int(11) NOT NULL,
  `cuenta` varchar(16) NOT NULL,
  `mes` varchar(6) NOT NULL,
  PRIMARY KEY (`banco`,`cuenta`,`mes`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
