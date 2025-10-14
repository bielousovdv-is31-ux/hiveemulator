kubectl create secret docker-registry gitlab-registry-secret \
--docker-server=registry.gitlab.com \
--docker-username=kpi-demo-k8s-1 \
--docker-password=glpat-TyzDAx1reg3xPgMgzSe5 \
--docker-email=zotkin.kv@pm.me \
--namespace=default
