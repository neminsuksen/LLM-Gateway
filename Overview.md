OPENAI_API_KEY เก็บ Key ของ OpenAI
gpt-5-nano  Model ที่ถูกที่สุดนำมาใช้ทดสอบ ที่ openai ตอบมาคือ gpt-5-nano-2025-08-07
claude-haiku-5-5 Model ที่ถูกที่สุดฝั่ง Antropic

Add API Key
[Environment]::SetEnvironmentVariable("ANTHROPIC_API_KEY", (Read-Host "key"), "User")   เปลี่ยนชื่อตาม Key ANTHROPIC_API_KEY|OPENAI_API_KEY
$env:ANTHROPIC_API_KEY = [Environment]::GetEnvironmentVariable("ANTHROPIC_API_KEY", "User")  โหลดค่าเข้าไปใช้เลยไม่ต้องรอปิดเปิดใหม่
$env:ANTHROPIC_API_KEY.Length   เช็คความยาวดูว่า Key เข้ามั้ยโดยไม่อ่านตัวคีย์ออกมาดู
