all:
	xbuild

dist: r2w32
	xbuild /p:Configuration=Release
	rm -rf r2wars
	mkdir -p r2wars
	cp -rf bin/Release/*.exe bin/Release/*.dll README.md r2wars
	cp -rf examples r2wars
	zip -r r2wars.zip r2wars
	rm -rf r2wars

r2w32:
	wget -c http://radare.mikelloc.com/get/2.7.0/radare2_installer-msvc_64-2.7.0.exe

clean:
	rm -rf bin obj r2wars r2wars.zip
