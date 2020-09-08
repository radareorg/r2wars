module main

import r2wars
import flag
import os

fn main() {
	mut fp := flag.new_flag_parser(os.args[1..])
	fp.application('r2wars')
	fp.version(r2wars.version)
	fp.description('battle assembly bots')
	fq := fp.bool('quiet', `q`, false, 'Do not show live battles')
	fv := fp.bool('version', `v`, false, 'Show version')
	fh := fp.bool('help', `h`, false, 'Show this help')
	if fv {
		println('${r2wars.version}')
		return
	}
	if fh {
		println(fp.usage())
		return
	}
	mut war := r2wars.War{}
	botsdir := '../examples'
	// botsdir := '../examples/r2con2020/round1'
	// botsdir := 'bots'
	war.verbose = !fq
	war.load_bots(botsdir)
	war.fight() or {
		panic(err)
	}
	war.free()
}
