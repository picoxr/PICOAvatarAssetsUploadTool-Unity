
// from [ARM:astc-encoder] quantization_and_transfer_table quant_and_xfer_tables
#define WEIGHT_QUANTIZE_NUM 32
static const int scramble_table[12 * WEIGHT_QUANTIZE_NUM] = {
	// quantization method 0, range 0..1
	//{
		0, 1,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	//},
	// quantization method 1, range 0..2
	//{
		0, 1, 2,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	//},
	// quantization method 2, range 0..3
	//{
		0, 1, 2, 3,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	//},
	// quantization method 3, range 0..4
	//{
		0, 1, 2, 3, 4,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	//},
	// quantization method 4, range 0..5
	//{
		0, 2, 4, 5, 3, 1,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	//},
	// quantization method 5, range 0..7
	//{
		0, 1, 2, 3, 4, 5, 6, 7,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	//},
	// quantization method 6, range 0..9
	//{
		0, 2, 4, 6, 8, 9, 7, 5, 3, 1,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	//},
	// quantization method 7, range 0..11
	//{
		0, 4, 8, 2, 6, 10, 11, 7, 3, 9, 5, 1,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	//},
	// quantization method 8, range 0..15
	//{
		0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	//},
	// quantization method 9, range 0..19
	//{
		0, 4, 8, 12, 16, 2, 6, 10, 14, 18, 19, 15, 11, 7, 3, 17, 13, 9, 5, 1,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	//},
	// quantization method 10, range 0..23
	//{
		0, 8, 16, 2, 10, 18, 4, 12, 20, 6, 14, 22, 23, 15, 7, 21, 13, 5, 19,
		11, 3, 17, 9, 1, 0, 0, 0, 0, 0, 0, 0, 0,
	//},
	// quantization method 11, range 0..31
	//{
		0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
		20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31,
	//}

};

