import asyncio
from openai import AsyncOpenAI

BASE_URL = "https://chatapi.littlewheat.com/v1"
API_KEY = "sk-XXXX"
MODEL = "gpt-4o-mini"

async def main():
    client = AsyncOpenAI(base_url=BASE_URL, api_key=API_KEY)
    resp = await client.responses.create(
        model=MODEL,
        instructions="You are good at telling jokes.",
        input="Tell me a joke about a pirate.",
    )
    print(resp.output_text)

asyncio.run(main())
