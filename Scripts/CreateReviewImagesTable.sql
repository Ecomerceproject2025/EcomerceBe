-- Create ReviewImages table if it doesn't exist
CREATE TABLE IF NOT EXISTS `ReviewImages` (
    `ReviewImageId` INT NOT NULL AUTO_INCREMENT,
    `ReviewId` INT NOT NULL,
    `ImageUrl` LONGTEXT NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    PRIMARY KEY (`ReviewImageId`),
    CONSTRAINT `FK_ReviewImages_Reviews_ReviewId` 
        FOREIGN KEY (`ReviewId`) 
        REFERENCES `Reviews` (`ReviewId`) 
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Create index for better query performance
CREATE INDEX IF NOT EXISTS `IX_ReviewImages_ReviewId` ON `ReviewImages` (`ReviewId`);

