select * from Usuarios

UPDATE usuarios 
SET password_hash = 'paola123' 
WHERE username = 'admin';