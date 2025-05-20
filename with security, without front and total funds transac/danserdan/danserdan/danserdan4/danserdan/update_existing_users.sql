-- Update existing users with default values for firstName and lastName
UPDATE users
SET firstName = 'User', lastName = 'Account'
WHERE firstName = '' OR firstName IS NULL;
