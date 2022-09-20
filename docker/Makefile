all:
	docker images | grep -q ^r2wars || docker build --no-cache -t r2wars .
	bash -c "trap './start.sh' EXIT;"

clean:
	docker rmi r2wars
