class ELIFE_CharacterDto : JsonApiStruct
{
	string id;
	string firstName;
	string lastName;

	void ELIFE_CharacterDto()
	{
		RegV("id");
		RegV("firstName");
		RegV("lastName");
	}
}
