Name pool files are comma-separated UTF-8 text lists used by fictional player and coach generation.

male_first_names.txt: masculine first-name pool
female_first_names.txt: feminine first-name pool
last_names.txt: shared surname pool

Keep each name unique within its file. The GameCore generator will load these pools in a future data-loading slice.

The pools use U.S.-ranked entries from the categorized names-dataset library. First names are screened before being placed in a gendered pool. Surnames use the same ranking with soft initial caps, avoiding both rare forced entries and an obvious overrepresentation of one initial.
