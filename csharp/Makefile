VERSION=$(shell ../version.sh)
R2W=r2wars-$(VERSION)
DOTNET?=dotnet

all:
	$(DOTNET) build r2wars.sln

run:
	$(DOTNET) run --project r2wars.csproj -- ../warriors

ls:
	$(SHELL) lsbots.sh

dist:
	rm -rf $(R2W)
	$(DOTNET) publish r2wars.csproj --configuration Release --output $(R2W) --no-self-contained
	cp -f ../RELEASE_README.md $(R2W)/README.txt
	mkdir -p $(R2W)/warriors
	cp -rf ../examples/. $(R2W)/warriors
	zip -r $(R2W).zip $(R2W)
	rm -rf $(R2W)

r2w32:
	wget -c http://radare.mikelloc.com/get/2.7.0/radare2_installer-msvc_64-2.7.0.exe

clean:
	$(DOTNET) clean r2wars.sln
	rm -rf bin obj r2wars-*.zip
