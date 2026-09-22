class ELIFE_CharacterDtoListResultDto : JsonApiStruct
{
	ref array<ref ELIFE_MessageDto> messages = {};
	ref array<ref ELIFE_CharacterDto> data = {};

	void ELIFE_CharacterDtoListResultDto()
	{
		RegV("messages");
		RegV("data");
	}
}
