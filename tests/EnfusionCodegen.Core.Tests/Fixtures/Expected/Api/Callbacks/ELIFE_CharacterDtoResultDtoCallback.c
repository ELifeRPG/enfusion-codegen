class ELIFE_CharacterDtoResultDtoCallback : ELIFE_BaseRestCallback
{
	override ELIFE_EApiStatusCode ExtractData(string data, int dataSize, out JsonApiStruct resultData)
	{
		resultData = new CharacterDtoResultDto();
		resultData.ExpandFromRAW(data);
		if (!resultData)
			return ELIFE_EApiStatusCode.ERROR;
		return ELIFE_EApiStatusCode.SUCCESS;
	}
}
