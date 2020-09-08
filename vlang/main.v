module main

import r2wars

fn main() {
	mut war := r2wars.new()
	botsdir := '../examples'
	// botsdir := '../examples/r2con2020/round1'
	// botsdir := 'bots'
	war.load_bots(botsdir)
	war.fight() or {
		panic(err)
	}
	war.free()
}
