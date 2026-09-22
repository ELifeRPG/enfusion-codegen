modded class ELIFE_Api
{
	void CreateCharacter(ELIFE_CharacterDto body, Managed instance = null, string functionName = "")
	{
		ELIFE_CharacterDtoResultDtoCallback cbx = new ELIFE_CharacterDtoResultDtoCallback;
		cbx.SetCallback(instance, functionName);
		body.Pack();
		GetElifeApi().POST(cbx, "characters", body.AsString());
	}

	void CreateCharacterSession(string characterId, Managed instance = null, string functionName = "")
	{
		ELIFE_CharacterDtoResultDtoCallback cbx = new ELIFE_CharacterDtoResultDtoCallback;
		cbx.SetCallback(instance, functionName, characterId);
		GetElifeApi().POST(cbx, string.Format("characters/%1/sessions", characterId), "");
	}
}
