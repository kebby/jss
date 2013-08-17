state = [];
note = [];
velo = [];
gate = [];

values = [
//!VALUES
];

ticklen = 20000;
tickcnt = 0;

function render(buffer, len)
{
	var s = 0;
	while (s < 2*len)
	{
		var ll = 0, rr = 0, c = 0, l, r, i, v;

		// sequencer
		if (tickcnt == 0)
		{
			tickcnt = ticklen;

			// FAKE: trigger new note
			note[0] = 48;
			state[0] = values[0].slice();
		}
		tickcnt--;

		//!CODE

		buffer[s++] = ll;
		buffer[s++] = rr;
	}
	
}

function render2(len) {
	var out = [];
	render(out, len);
	return out;
}
