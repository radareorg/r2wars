all:
	docker images | grep -q ^r2wars || docker build --no-cache -t r2wars .
	sh start.sh

clean:
	docker rmi r2wars
